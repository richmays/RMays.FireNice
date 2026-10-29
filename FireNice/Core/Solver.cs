namespace FireNice.Core;

/// <summary>A single key press the solver can make.</summary>
public enum SolverInput
{
    Left,
    Right,
    Up,
    Down,
    Action,
}

public enum SolveOutcome
{
    /// <summary>A shortest solution was found.</summary>
    Solved,

    /// <summary>
    /// The shortest search ran out of time, but the fallback search found a solution. There may be
    /// a shorter one.
    /// </summary>
    SolvedNotShortest,

    /// <summary>Every reachable position was checked and none puts out all the fires.</summary>
    Impossible,

    /// <summary>The time limit (or the size limit) was reached before either search found a solution.</summary>
    GaveUp,

    /// <summary>There were no fires left to begin with.</summary>
    AlreadySolved,
}

public sealed record SolveResult(SolveOutcome Outcome, IReadOnlyList<SolverInput> Inputs, int PositionsChecked);

/// <summary>
/// Finds the fewest key presses that put out every fire, starting from a game as it is now. If
/// that takes too long, falls back to finding any solution.
/// </summary>
/// <remarks>
/// The main search is breadth-first: it tries every key press from the current position, lets
/// everything settle (falling, sliding and pipe travel run until nothing moves), and remembers each
/// new position so it's never checked twice. Positions are explored in order of how many key
/// presses it took to reach them, so the first win found is a shortest one. Because it waits for
/// things to settle between presses, it won't find solutions that need a key pressed mid-fall or
/// mid-slide. The fallback is a greedy best-first search (see <see cref="SolveAny"/>).
/// </remarks>
public static class Solver
{
    private const int ChunkSize = 512;

    private static readonly SolverInput[] AllInputs =
    {
        SolverInput.Left, SolverInput.Right, SolverInput.Up, SolverInput.Down, SolverInput.Action,
    };

    /// <summary>Presses one key on the game.</summary>
    public static void Apply(GameState game, SolverInput input)
    {
        switch (input)
        {
            case SolverInput.Left:
                game.Press(Direction.Left);
                break;
            case SolverInput.Right:
                game.Press(Direction.Right);
                break;
            case SolverInput.Up:
                game.PressUp();
                break;
            case SolverInput.Down:
                game.PressDown();
                break;
            case SolverInput.Action:
                game.UseAction();
                break;
        }
    }

    /// <summary>
    /// Ticks until nothing moves. Returns false if it never settles (for example, a loop of pipes).
    /// </summary>
    public static bool Settle(GameState game, int maxTicks = 2000)
    {
        int ticks = 0;
        while (game.Tick())
        {
            if (++ticks > maxTicks)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Searches for a solution. The game passed in isn't changed. The shortest search runs until
    /// <paramref name="shortestTimeLimit"/> (or until <paramref name="cancel"/> if there's no limit);
    /// if it gives up, the fallback search runs until <paramref name="cancel"/>. Stops with
    /// <see cref="SolveOutcome.GaveUp"/> if the token is cancelled or too many positions pile up first.
    /// <paramref name="isDeadEnd"/>, if given, marks settled positions that can't lead to a win (see
    /// <see cref="LevelShortcuts"/>); neither search goes on from them.
    /// </summary>
    public static SolveResult Solve(
        GameState start,
        CancellationToken cancel,
        TimeSpan? shortestTimeLimit = null,
        int maxPositions = 25_000_000,
        Func<GameState, bool>? isDeadEnd = null)
    {
        GameState root = start.Clone();
        if (!Settle(root) || root.Status == GameStatus.Lost)
        {
            return new SolveResult(SolveOutcome.Impossible, Array.Empty<SolverInput>(), 1);
        }

        if (root.Status == GameStatus.Won)
        {
            return new SolveResult(SolveOutcome.AlreadySolved, Array.Empty<SolverInput>(), 1);
        }

        SolveResult shortest;
        using (var shortestCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel))
        {
            if (shortestTimeLimit <= TimeSpan.Zero)
            {
                shortestCancel.Cancel();
            }
            else if (shortestTimeLimit is TimeSpan limit)
            {
                shortestCancel.CancelAfter(limit);
            }

            shortest = SolveShortest(root, shortestCancel.Token, maxPositions, isDeadEnd);
        }

        if (shortest.Outcome != SolveOutcome.GaveUp || cancel.IsCancellationRequested)
        {
            return shortest;
        }

        SolveResult any = SolveAny(root, cancel, maxPositions, isDeadEnd);
        return any with { PositionsChecked = shortest.PositionsChecked + any.PositionsChecked };
    }

    /// <summary>
    /// The breadth-first search for a shortest solution, from a settled game that's still playing.
    /// </summary>
    /// <remarks>
    /// Positions are stored packed into a few 64-bit words each (see <see cref="GameState.Pack"/>),
    /// and one scratch game per thread is loaded from them, so no game is ever cloned per press.
    /// Each layer (all positions the same number of presses from the start) is expanded in
    /// parallel, then the new positions are added in exactly the order a one-at-a-time search would
    /// add them, so the solution found is the same every time.
    /// </remarks>
    private static SolveResult SolveShortest(
        GameState root, CancellationToken cancel, int maxPositions, Func<GameState, bool>? isDeadEnd)
    {
        int[] cells = ChangeableCells(root.Level);
        var positions = new PositionStore(PackedWords(cells));
        Span<ulong> packed = stackalloc ulong[positions.Words];
        root.Pack(cells, packed);
        positions.TryAdd(packed, -1, default);

        int layerStart = 0;
        while (layerStart < positions.Count)
        {
            int layerEnd = positions.Count;
            int chunkCount = (layerEnd - layerStart + ChunkSize - 1) / ChunkSize;
            var found = new Found[chunkCount];
            Parallel.For(
                0,
                chunkCount,
                () => root.Clone(),
                (chunk, loop, scratch) =>
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        int from = layerStart + (chunk * ChunkSize);
                        found[chunk] = Expand(scratch, cells, positions, from, Math.Min(from + ChunkSize, layerEnd), isDeadEnd);
                    }

                    return scratch;
                },
                _ => { });

            if (cancel.IsCancellationRequested)
            {
                return new SolveResult(SolveOutcome.GaveUp, Array.Empty<SolverInput>(), positions.Count);
            }

            // Add the new positions in order: by the position they came from, then by key.
            foreach (Found chunk in found)
            {
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (!positions.TryAdd(chunk.Packed(i), chunk.Parents[i], chunk.Inputs[i]))
                    {
                        continue; // Reached earlier in this layer.
                    }

                    if (chunk.Won[i])
                    {
                        return new SolveResult(SolveOutcome.Solved, positions.PathTo(positions.Count - 1), positions.Count);
                    }
                }
            }

            if (positions.Count > maxPositions)
            {
                return new SolveResult(SolveOutcome.GaveUp, Array.Empty<SolverInput>(), positions.Count);
            }

            layerStart = layerEnd;        }

        return new SolveResult(SolveOutcome.Impossible, Array.Empty<SolverInput>(), positions.Count);
    }

    /// <summary>
    /// The fallback: a greedy best-first search for any solution, from a settled game that's still
    /// playing. It always tries the most promising position found so far next (fewest fires left,
    /// then ice and crates closest to the fires; ties go to the position found first), so it finds a
    /// solution much sooner than the shortest search on most levels, but often not a shortest one.
    /// </summary>
    internal static SolveResult SolveAny(
        GameState root, CancellationToken cancel, int maxPositions, Func<GameState, bool>? isDeadEnd = null)
    {
        int[] cells = ChangeableCells(root.Level);
        var positions = new PositionStore(PackedWords(cells));
        var packed = new ulong[positions.Words];
        var parent = new ulong[positions.Words];
        root.Pack(cells, packed);
        positions.TryAdd(packed, -1, default);

        var open = new PriorityQueue<int, (int Score, int Node)>();
        open.Enqueue(0, (Promise(root), 0));
        GameState scratch = root.Clone();
        while (open.TryDequeue(out int node, out _))
        {
            if (cancel.IsCancellationRequested || positions.Count > maxPositions)
            {
                return new SolveResult(SolveOutcome.GaveUp, Array.Empty<SolverInput>(), positions.Count);
            }

            positions.Get(node).CopyTo(parent);
            foreach (SolverInput input in AllInputs)
            {
                scratch.Unpack(cells, parent);
                if (!scratch.CanAct || !CanDoAnything(scratch, input))
                {
                    continue;
                }

                Apply(scratch, input);
                if (!Settle(scratch) || scratch.Status == GameStatus.Lost || IsDeadEnd(scratch, isDeadEnd))
                {
                    continue;
                }

                scratch.Pack(cells, packed);
                if (!positions.TryAdd(packed, node, input))
                {
                    continue;
                }

                int added = positions.Count - 1;
                if (scratch.Status == GameStatus.Won)
                {
                    return new SolveResult(SolveOutcome.SolvedNotShortest, positions.PathTo(added), positions.Count);
                }

                open.Enqueue(added, (Promise(scratch), added));
            }
        }

        return new SolveResult(SolveOutcome.Impossible, Array.Empty<SolverInput>(), positions.Count);
    }

    private static bool IsDeadEnd(GameState game, Func<GameState, bool>? isDeadEnd) =>
        isDeadEnd is not null && game.Status != GameStatus.Won && isDeadEnd(game);

    /// <summary>
    /// How far a position looks from winning (lower is better): a large penalty per fire left, plus
    /// how far each ice cube and crate is from the nearest fire.
    /// </summary>
    private static int Promise(GameState game)
    {
        Level level = game.Level;
        var fires = new List<(int X, int Y)>();
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (level[x, y].Type == CellType.Fire)
                {
                    fires.Add((x, y));
                }
            }
        }

        int score = fires.Count * 1000;
        if (fires.Count == 0)
        {
            return score;
        }

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                Cell cell = level[x, y];
                if (!cell.IsIce && cell.Type != CellType.Crate)
                {
                    continue;
                }

                int nearest = int.MaxValue;
                foreach (var (fx, fy) in fires)
                {
                    nearest = Math.Min(nearest, Math.Abs(fx - x) + Math.Abs(fy - y));
                }

                score += nearest;
            }
        }

        return score;
    }

    /// <summary>The cells that can ever change. Blocks and pipes never move and nothing replaces them.</summary>
    private static int[] ChangeableCells(Level level)
    {
        var cells = new List<int>();
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                Cell cell = level[x, y];
                if (cell.Type != CellType.Block && !cell.IsPipe)
                {
                    cells.Add((y * level.Width) + x);
                }
            }
        }

        return cells.ToArray();
    }

    private static int PackedWords(int[] cells) => (GameState.PackedBits(cells.Length) + 63) / 64;

    /// <summary>Tries every key from each position in [from, to) and collects the positions not seen before.</summary>
    private static Found Expand(
        GameState scratch, int[] cells, PositionStore positions, int from, int to, Func<GameState, bool>? isDeadEnd)
    {
        var found = new Found(positions.Words);
        Span<ulong> packed = stackalloc ulong[positions.Words];
        for (int node = from; node < to; node++)
        {
            ReadOnlySpan<ulong> parent = positions.Get(node);
            scratch.Unpack(cells, parent);
            if (!scratch.CanAct)
            {
                continue; // Waiting inside a blocked pipe: every key is ignored.
            }

            bool fresh = true;
            foreach (SolverInput input in AllInputs)
            {
                if (!fresh)
                {
                    scratch.Unpack(cells, parent);
                }

                if (!CanDoAnything(scratch, input))
                {
                    fresh = true;
                    continue;
                }

                fresh = false;
                Apply(scratch, input);
                if (!Settle(scratch) || scratch.Status == GameStatus.Lost || IsDeadEnd(scratch, isDeadEnd))
                {
                    continue;
                }

                scratch.Pack(cells, packed);
                if (!positions.Contains(packed))
                {
                    found.Add(packed, node, input, scratch.Status == GameStatus.Won);
                }
            }
        }

        return found;
    }

    /// <summary>
    /// False when the key is certain to be ignored, so it isn't worth trying. Up and Down only do
    /// something next to a pipe opening that way; Action only works on air or ice.
    /// </summary>
    private static bool CanDoAnything(GameState game, SolverInput input)
    {
        Player player = game.Player;
        switch (input)
        {
            case SolverInput.Up:
                return game.Level[player.X, player.Y - 1].IsOpen(Heading.Down);
            case SolverInput.Down:
                return game.Level[player.X, player.Y + 1].IsOpen(Heading.Up);
            case SolverInput.Action:
                var (x, y) = game.ActionTarget;
                Cell target = game.Level[x, y];
                return target.Type == CellType.Air || target.IsIce;
            default:
                return true;
        }
    }

    /// <summary>New positions found while expanding one chunk, in the order they were found.</summary>
    private sealed class Found
    {
        private readonly int words;
        private ulong[] packed;

        public Found(int words)
        {
            this.words = words;
            packed = new ulong[words * 64];
        }

        public int Count { get; private set; }

        public List<int> Parents { get; } = new();

        public List<SolverInput> Inputs { get; } = new();

        public List<bool> Won { get; } = new();

        public ReadOnlySpan<ulong> Packed(int i) => packed.AsSpan(i * words, words);

        public void Add(ReadOnlySpan<ulong> position, int parent, SolverInput input, bool won)
        {
            if ((Count + 1) * words > packed.Length)
            {
                Array.Resize(ref packed, packed.Length * 2);
            }

            position.CopyTo(packed.AsSpan(Count * words));
            Parents.Add(parent);
            Inputs.Add(input);
            Won.Add(won);
            Count++;
        }
    }

    /// <summary>
    /// Every position found so far, in the order found, with how each was reached. Lookups are an
    /// open-addressing hash table of position numbers. Contains is safe to call from many threads
    /// as long as nothing is being added.
    /// </summary>
    private sealed class PositionStore
    {
        private ulong[] packed;
        private int[] parents;
        private byte[] inputs;
        private int[] table; // Position number + 1, or 0 for an empty slot.

        public PositionStore(int words)
        {
            Words = words;
            packed = new ulong[words * 1024];
            parents = new int[1024];
            inputs = new byte[1024];
            table = new int[4096];
        }

        public int Words { get; }

        public int Count { get; private set; }

        public ReadOnlySpan<ulong> Get(int node) => packed.AsSpan(node * Words, Words);

        public bool Contains(ReadOnlySpan<ulong> position)
        {
            int mask = table.Length - 1;
            for (int slot = Hash(position) & mask; ; slot = (slot + 1) & mask)
            {
                int entry = table[slot];
                if (entry == 0)
                {
                    return false;
                }

                if (Get(entry - 1).SequenceEqual(position))
                {
                    return true;
                }
            }
        }

        public bool TryAdd(ReadOnlySpan<ulong> position, int parent, SolverInput input)
        {
            int mask = table.Length - 1;
            int slot = Hash(position) & mask;
            for (; table[slot] != 0; slot = (slot + 1) & mask)
            {
                if (Get(table[slot] - 1).SequenceEqual(position))
                {
                    return false;
                }
            }

            if (Count == parents.Length)
            {
                int size = Count * 2;
                Array.Resize(ref packed, size * Words);
                Array.Resize(ref parents, size);
                Array.Resize(ref inputs, size);
            }

            position.CopyTo(packed.AsSpan(Count * Words));
            parents[Count] = parent;
            inputs[Count] = (byte)input;
            table[slot] = ++Count;
            if (Count * 2 > table.Length)
            {
                Rehash();
            }

            return true;
        }

        public List<SolverInput> PathTo(int node)
        {
            var path = new List<SolverInput>();
            while (parents[node] >= 0)
            {
                path.Add((SolverInput)inputs[node]);
                node = parents[node];
            }

            path.Reverse();
            return path;
        }

        private static int Hash(ReadOnlySpan<ulong> position)
        {
            ulong hash = 0x9E3779B97F4A7C15;
            foreach (ulong word in position)
            {
                hash = (hash ^ word) * 0xBF58476D1CE4E5B9;
                hash ^= hash >> 31;
            }

            return (int)(hash ^ (hash >> 32)) & int.MaxValue;
        }

        private void Rehash()
        {
            table = new int[table.Length * 2];
            int mask = table.Length - 1;
            for (int node = 0; node < Count; node++)
            {
                int slot = Hash(Get(node)) & mask;
                while (table[slot] != 0)
                {
                    slot = (slot + 1) & mask;
                }

                table[slot] = node + 1;
            }
        }
    }
}
