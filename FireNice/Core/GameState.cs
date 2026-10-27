namespace FireNice.Core;

/// <summary>
/// All of the game rules. The UI calls <see cref="Tick"/> on a timer (about every 100 ms);
/// tests call it directly. See FireNicePlan.md section 3 for the rules this implements.
/// </summary>
public sealed class GameState : IPlayerControls
{
    private readonly List<Slider> sliding = new();

    private readonly List<CellMove> moves = new();

    private readonly bool startsWithFire;

    /// <summary>Set while the player is traveling through a pipe.</summary>
    private PipeTravel? pipe;

    // Rewind: one point per input since the level started, the latest on top.
    private readonly Stack<RewindPoint> rewindPoints = new();
    private readonly List<RewindStep> rewindPath = new();

    public GameState(LevelData data)
    {
        Data = data;
        startsWithFire = new Level(data.CopyCells()).CountFires() > 0;
        KeepsRewind = true;
        Reset();
    }

    /// <summary>A separate copy of the game exactly as it is now, including anything mid-motion.</summary>
    private GameState(GameState other)
    {
        Data = other.Data;
        startsWithFire = other.startsWithFire;
        Level = new Level(other.Level.CopyCells());
        Player = other.Player.Clone();
        sliding.AddRange(other.sliding.Select(s => s.Clone()));
        pipe = other.pipe?.Clone();
        Status = other.Status;
        LastChange = other.LastChange;
    }

    /// <summary>
    /// Copies the game so it can be explored (for example by the solver) without changing this one.
    /// The copy doesn't keep a rewind step, so exploring it stays fast.
    /// </summary>
    public GameState Clone() => new(this);

    /// <summary>
    /// A text fingerprint of the game when nothing is moving: every cell, plus the player's
    /// position, facing, and pipe state. Two games with the same key play out identically.
    /// </summary>
    internal string StateKey()
    {
        int cellCount = Level.Width * Level.Height;
        return string.Create(cellCount + 6, this, (chars, game) =>
        {
            int i = 0;
            for (int y = 0; y < game.Level.Height; y++)
            {
                for (int x = 0; x < game.Level.Width; x++)
                {
                    chars[i++] = CellChars.ToChar(game.Level[x, y]);
                }
            }

            chars[i++] = (char)game.Player.X;
            chars[i++] = (char)game.Player.Y;
            chars[i++] = game.Player.Facing == Direction.Left ? 'l' : 'r';
            chars[i++] = game.Player.IsAlive ? 'a' : 'd';
            chars[i++] = game.pipe is null ? '-' : (char)('0' + (int)game.pipe.Moving);
            chars[i] = game.pipe?.Returning == true ? 'b' : 'f';
        });
    }

    /// <summary>
    /// Solver support: packs a settled game into bits. Only the cells listed can change (blocks and
    /// pipes never do), so only those are stored, 4 bits each, followed by the player and pipe state.
    /// Two games with the same bits play out identically, like <see cref="StateKey"/>.
    /// </summary>
    internal void Pack(ReadOnlySpan<int> cellIndexes, Span<ulong> bits)
    {
        bits.Clear();
        int bit = 0;
        int width = Level.Width;
        foreach (int index in cellIndexes)
        {
            WriteBits(bits, ref bit, 4, CellCode(Level[index % width, index / width]));
        }

        WriteBits(bits, ref bit, 8, (ulong)Player.X);
        WriteBits(bits, ref bit, 8, (ulong)Player.Y);
        WriteBits(bits, ref bit, 1, Player.Facing == Direction.Left ? 1UL : 0UL);
        WriteBits(bits, ref bit, 1, Player.IsAlive ? 1UL : 0UL);
        WriteBits(bits, ref bit, 3, pipe is null ? 0UL : 1UL + (ulong)pipe.Moving);
        WriteBits(bits, ref bit, 1, pipe?.Returning == true ? 1UL : 0UL);
    }

    /// <summary>Solver support: makes this game the settled position that <see cref="Pack"/> wrote.</summary>
    internal void Unpack(ReadOnlySpan<int> cellIndexes, ReadOnlySpan<ulong> bits)
    {
        int bit = 0;
        int width = Level.Width;
        foreach (int index in cellIndexes)
        {
            Level[index % width, index / width] = CodeCell(ReadBits(bits, ref bit, 4));
        }

        Player.X = (int)ReadBits(bits, ref bit, 8);
        Player.Y = (int)ReadBits(bits, ref bit, 8);
        Player.Facing = ReadBits(bits, ref bit, 1) == 1 ? Direction.Left : Direction.Right;
        Player.IsAlive = ReadBits(bits, ref bit, 1) == 1;
        int pipeCode = (int)ReadBits(bits, ref bit, 3);
        bool returning = ReadBits(bits, ref bit, 1) == 1;
        pipe = pipeCode == 0 ? null : new PipeTravel((Heading)(pipeCode - 1)) { Returning = returning };
        sliding.Clear();
        moves.Clear();
        Status = GameStatus.Playing;
    }

    /// <summary>How many bits <see cref="Pack"/> writes for the given number of cells.</summary>
    internal static int PackedBits(int cellCount) => (cellCount * 4) + 22;

    private static ulong CellCode(Cell cell) => cell.Type switch
    {
        CellType.Air => 0,
        CellType.Fire => 1,
        CellType.Crate => 2,
        CellType.UnlitJar => 3,
        CellType.LitJar => 4,
        CellType.Ice => 5UL + (cell.AttachedLeft ? 1UL : 0UL) + (cell.AttachedRight ? 2UL : 0UL),
        _ => throw new InvalidOperationException($"{cell.Type} cells never change, so they aren't packed."),
    };

    private static Cell CodeCell(ulong code) => code switch
    {
        0 => Cell.Air,
        1 => Cell.Fire,
        2 => Cell.Crate,
        3 => Cell.UnlitJar,
        4 => Cell.LitJar,
        _ => Cell.Ice(attachedLeft: ((code - 5) & 1) != 0, attachedRight: ((code - 5) & 2) != 0),
    };

    private static void WriteBits(Span<ulong> bits, ref int bit, int count, ulong value)
    {
        int offset = bit & 63;
        bits[bit >> 6] |= value << offset;
        if (offset + count > 64)
        {
            bits[(bit >> 6) + 1] |= value >> (64 - offset);
        }

        bit += count;
    }

    private static ulong ReadBits(ReadOnlySpan<ulong> bits, ref int bit, int count)
    {
        int offset = bit & 63;
        ulong value = bits[bit >> 6] >> offset;
        if (offset + count > 64)
        {
            value |= bits[(bit >> 6) + 1] << (64 - offset);
        }

        bit += count;
        return value & ((1UL << count) - 1);
    }

    public event EventHandler? StateChanged;

    public LevelData Data { get; }

    public Level Level { get; private set; } = null!;

    public Player Player { get; private set; } = null!;

    public GameStatus Status { get; private set; }

    public int FiresLeft => Level.CountFires();

    /// <summary>What caused the latest <see cref="StateChanged"/>; lets the UI choose how to animate.</summary>
    public ChangeKind LastChange { get; private set; }

    /// <summary>
    /// Ice, fire and crates that moved during the latest tick, so the UI can animate them.
    /// After a rewind, each thing that moved back, from where it was to where it is now.
    /// </summary>
    public IReadOnlyList<CellMove> LastMoves => moves;

    /// <summary>
    /// Whether inputs are remembered so <see cref="Rewind"/> can undo them. On for a new game;
    /// off for copies made with <see cref="Clone"/>.
    /// </summary>
    public bool KeepsRewind { get; set; }

    /// <summary>There's an input that <see cref="Rewind"/> can undo.</summary>
    public bool CanRewind => rewindPoints.Count > 0;

    /// <summary>How many inputs <see cref="Rewind"/> can undo, one at a time.</summary>
    public int RewindCount => rewindPoints.Count;

    /// <summary>
    /// After a rewind: the cells the player went back through, starting where they were and
    /// ending where they are now.
    /// </summary>
    public IReadOnlyList<RewindStep> LastRewindPath => rewindPath;

    /// <summary>The player has nothing under them and is dropping.</summary>
    public bool IsPlayerFalling => Player.IsAlive && !IsInPipe && Level[Player.X, Player.Y + 1].IsEmpty;

    /// <summary>The player is inside a pipe, moving through it automatically.</summary>
    public bool IsInPipe => pipe is not null;

    /// <summary>Input is only accepted while playing and standing on something (not falling or in a pipe).</summary>
    public bool CanAct => Status == GameStatus.Playing && !IsPlayerFalling && !IsInPipe;

    /// <summary>The cell the action button works on: one ahead and one down.</summary>
    public (int X, int Y) ActionTarget => (Player.X + Player.Facing.Dx(), Player.Y + 1);

    public bool IsSliding(int x, int y)
    {
        // A plain loop: this runs for every falling cell on every tick, so it shouldn't allocate.
        foreach (Slider s in sliding)
        {
            if (s.X == x && s.Y == y)
            {
                return true;
            }
        }

        return false;
    }

    public void Reset()
    {
        Level = new Level(Data.CopyCells());
        Player = new Player(Data.PlayerStartX, Data.PlayerStartY);
        sliding.Clear();
        moves.Clear();
        pipe = null;
        Status = GameStatus.Playing;
        rewindPoints.Clear();
        OnChanged(ChangeKind.Reset);
    }

    /// <summary>
    /// Undoes the latest input and everything it set off (falls, slides, pipe trips, even a
    /// death), putting the game back exactly as it was just before that key press. Every input
    /// since the level started is kept, so rewinding again and again leads back to the start.
    /// Returns false if there's nothing left to undo.
    /// </summary>
    public bool Rewind()
    {
        if (!rewindPoints.TryPop(out RewindPoint? point))
        {
            return false;
        }

        rewindPath.Clear();
        for (int i = point.Trail.Count - 1; i >= 0; i--)
        {
            rewindPath.Add(point.Trail[i]);
        }

        moves.Clear();
        foreach (var (now, origin) in point.Origins)
        {
            moves.Add(new CellMove(now.X, now.Y, origin.X, origin.Y));
        }

        GameState before = point.Before;
        Level = before.Level;
        Player = before.Player;
        sliding.Clear();
        sliding.AddRange(before.sliding);
        pipe = before.pipe;
        Status = before.Status;
        OnChanged(ChangeKind.Rewind);
        return true;
    }

    /// <summary>Up enters a pipe directly above that's open on its bottom.</summary>
    public void PressUp() => EnterPipeVertically(Heading.Up);

    /// <summary>Down enters a pipe directly below that's open on its top.</summary>
    public void PressDown() => EnterPipeVertically(Heading.Down);

    /// <summary>Turns the player, or steps / pushes / climbs if already facing that way.</summary>
    public void Press(Direction direction)
    {
        if (!CanAct)
        {
            return;
        }

        GameState? before = KeepsRewind ? Clone() : null;
        moves.Clear();
        if (Player.Facing != direction)
        {
            Player.Facing = direction;
        }
        else
        {
            Step(direction);
        }

        ApplyJars();
        UpdateStatus();
        RememberForRewind(before);
        OnChanged(ChangeKind.Input);
    }

    /// <summary>Creates ice in an empty target cell, or removes ice that's already there.</summary>
    public void UseAction()
    {
        if (!CanAct)
        {
            return;
        }

        var (x, y) = ActionTarget;
        Cell target = Level[x, y];
        if (target.Type != CellType.Air && !target.IsIce)
        {
            return;
        }

        GameState? before = KeepsRewind ? Clone() : null;
        moves.Clear();
        if (target.Type == CellType.Air)
        {
            CreateIce(x, y);
        }
        else
        {
            RemoveIce(x, y);
        }

        ApplyJars();
        UpdateStatus();
        RememberForRewind(before);
        OnChanged(ChangeKind.Input);
    }

    /// <summary>
    /// Advances the clock one step: the player moves one cell through a pipe, sliding things move,
    /// everything unsupported falls one cell, then jars act on whatever is on top of them.
    /// Returns true if anything changed.
    /// </summary>
    public bool Tick()
    {
        if (Status != GameStatus.Playing)
        {
            return false;
        }

        moves.Clear();
        bool changed = AdvanceThroughPipe();
        changed |= MoveSliding();
        changed |= ApplyGravity();
        changed |= ApplyJars();
        UpdateStatus();
        if (changed)
        {
            TrackForRewind(ChangeKind.Tick);
            OnChanged(ChangeKind.Tick);
        }

        return changed;
    }

    private void Step(Direction direction)
    {
        int tx = Player.X + direction.Dx();
        int ty = Player.Y;
        Cell target = Level[tx, ty];

        switch (target.Type)
        {
            case CellType.Air:
                MovePlayer(tx, ty);
                break;
            case CellType.Fire:
                MovePlayer(tx, ty);
                Player.IsAlive = false;
                break;
            case CellType.Ice or CellType.Crate when CanPush(tx, ty, direction):
                // The pushed thing moves on the next tick; the player stays put.
                sliding.Add(new Slider(tx, ty, direction));
                break;
            default:
                if (target.IsOpen(direction.ToHeading().Opposite()))
                {
                    EnterPipe(tx, ty, direction.ToHeading());
                }
                else
                {
                    TryClimb(tx, ty);
                }

                break;
        }
    }

    private void EnterPipeVertically(Heading heading)
    {
        if (!CanAct)
        {
            return;
        }

        int tx = Player.X;
        int ty = Player.Y + heading.Dy();
        if (!Level[tx, ty].IsOpen(heading.Opposite()))
        {
            return;
        }

        GameState? before = KeepsRewind ? Clone() : null;
        moves.Clear();
        EnterPipe(tx, ty, heading);
        UpdateStatus();
        RememberForRewind(before);
        OnChanged(ChangeKind.Input);
    }

    /// <summary>
    /// Pushes <paramref name="before"/> (the game just before an input) as the next step
    /// <see cref="Rewind"/> goes back to, unless the input didn't change anything (like walking
    /// into a wall).
    /// </summary>
    private void RememberForRewind(GameState? before)
    {
        if (before is null || (sliding.Count == before.sliding.Count && StateKey() == before.StateKey()))
        {
            return;
        }

        var point = new RewindPoint(before);
        point.Trail.Add(new RewindStep(before.Player.X, before.Player.Y, ChangeKind.Input));
        rewindPoints.Push(point);
        TrackForRewind(ChangeKind.Input);
    }

    /// <summary>
    /// Records where the player and anything that moved got to, so a rewind can glide them back.
    /// Everything that happens until the next input belongs to the latest one.
    /// </summary>
    private void TrackForRewind(ChangeKind cameBy)
    {
        if (!rewindPoints.TryPeek(out RewindPoint? point))
        {
            return;
        }

        RewindStep last = point.Trail[^1];
        if (Player.X != last.X || Player.Y != last.Y)
        {
            point.Trail.Add(new RewindStep(Player.X, Player.Y, cameBy));
        }

        if (moves.Count == 0)
        {
            return;
        }

        // Look up every origin before recording any, since one thing can move into the cell
        // another just left (a stack falling together).
        var arrived = new List<((int X, int Y) At, (int X, int Y) Origin)>(moves.Count);
        foreach (CellMove m in moves)
        {
            (int X, int Y) from = (m.FromX, m.FromY);
            arrived.Add(((m.ToX, m.ToY), point.Origins.Remove(from, out var origin) ? origin : from));
        }

        foreach (var (at, origin) in arrived)
        {
            point.Origins[at] = origin;
        }
    }

    private void EnterPipe(int x, int y, Heading moving)
    {
        MovePlayer(x, y);
        pipe = new PipeTravel(moving);
    }

    /// <summary>
    /// Moves the player one cell along the pipe they're in. Connected pipes are followed. At the
    /// far end the player comes out into air; if that's blocked they turn around and head back
    /// out the way they came. If the way back is blocked too, they wait inside until it clears.
    /// </summary>
    private bool AdvanceThroughPipe()
    {
        if (pipe is null)
        {
            return false;
        }

        Cell current = Level[Player.X, Player.Y];
        Openings exits = current.PipeOpenings & ~pipe.Moving.Opposite().ToOpening();
        Heading exit = exits switch
        {
            Openings.Up => Heading.Up,
            Openings.Down => Heading.Down,
            Openings.Left => Heading.Left,
            _ => Heading.Right,
        };

        int nx = Player.X + exit.Dx();
        int ny = Player.Y + exit.Dy();
        Cell next = Level[nx, ny];

        if (next.IsOpen(exit.Opposite()))
        {
            MovePlayer(nx, ny);
            pipe.Moving = exit;
            return true;
        }

        if (next.Type is CellType.Air or CellType.Fire)
        {
            MovePlayer(nx, ny);
            pipe = null;
            if (exit.IsHorizontal())
            {
                Player.Facing = exit == Heading.Left ? Direction.Left : Direction.Right;
            }

            if (next.Type == CellType.Fire)
            {
                Player.IsAlive = false;
            }

            return true;
        }

        if (pipe.Returning)
        {
            return false; // Both ways out are blocked; wait.
        }

        pipe.Returning = true;
        pipe.Moving = exit.Opposite();
        return true;
    }

    private bool CanPush(int x, int y, Direction direction)
    {
        int beyondX = x + direction.Dx();
        return Level[x, y].IsPushable
            && !IsAttachedCrate(x, y)
            && !IsSliding(x, y)
            && IsSupported(x, y)
            && Level[beyondX, y].IsEmpty
            && !IsPlayerAt(beyondX, y);
    }

    private void TryClimb(int tx, int ty)
    {
        if (Level[Player.X, Player.Y - 1].Type == CellType.Air && Level[tx, ty - 1].Type == CellType.Air)
        {
            MovePlayer(tx, ty - 1);
        }
    }

    private void CreateIce(int x, int y)
    {
        Cell left = Level[x - 1, y];
        Cell right = Level[x + 1, y];
        bool attachLeft = left.IsAnchor || left.IsIce;
        bool attachRight = right.IsAnchor || right.IsIce;
        Level[x, y] = Cell.Ice(attachLeft, attachRight);

        if (left.IsIce)
        {
            Level[x - 1, y] = left with { AttachedRight = true };
            StopSliding(x - 1, y);
        }

        if (right.IsIce)
        {
            Level[x + 1, y] = right with { AttachedLeft = true };
            StopSliding(x + 1, y);
        }
    }

    private void RemoveIce(int x, int y)
    {
        Level[x, y] = Cell.Air;
        StopSliding(x, y);
        DetachNeighbors(x, y);
    }

    /// <summary>The ice or crate that was at (x, y) is gone, so ice beside it can't be attached to it anymore.</summary>
    private void DetachNeighbors(int x, int y)
    {
        Cell left = Level[x - 1, y];
        if (left.IsIce && left.AttachedRight)
        {
            Level[x - 1, y] = left with { AttachedRight = false };
        }

        Cell right = Level[x + 1, y];
        if (right.IsIce && right.AttachedLeft)
        {
            Level[x + 1, y] = right with { AttachedLeft = false };
        }
    }

    /// <summary>
    /// Moves each pushed ice cube or crate one cell. Ice keeps sliding until it's stopped;
    /// a crate always makes its first move, then keeps sliding only while it's on ice.
    /// </summary>
    private bool MoveSliding()
    {
        if (sliding.Count == 0)
        {
            return false;
        }

        bool changed = false;
        foreach (Slider slider in sliding.ToList())
        {
            Cell thing = Level[slider.X, slider.Y];
            if (!thing.IsPushable || !IsSupported(slider.X, slider.Y))
            {
                // Gone, or ran out of floor: gravity takes over and it never slides again.
                sliding.Remove(slider);
                continue;
            }

            bool isCrate = thing.Type == CellType.Crate;
            if (isCrate && slider.Moves > 0 && !Level[slider.X, slider.Y + 1].IsIce)
            {
                sliding.Remove(slider);
                continue;
            }

            int nextX = slider.X + slider.Direction.Dx();
            Cell ahead = Level[nextX, slider.Y];
            if (ahead.Type == CellType.Fire && !isCrate)
            {
                Level[nextX, slider.Y] = Cell.Air;
                Level[slider.X, slider.Y] = Cell.Air;
                sliding.Remove(slider);
                changed = true;
            }
            else if (ahead.IsEmpty && !IsPlayerAt(nextX, slider.Y))
            {
                // A crate moving into fire puts it out and takes its place.
                Level[nextX, slider.Y] = thing;
                Level[slider.X, slider.Y] = Cell.Air;
                moves.Add(new CellMove(slider.X, slider.Y, nextX, slider.Y));
                slider.X = nextX;
                slider.Moves++;
                changed = true;
            }
            else
            {
                sliding.Remove(slider);
            }
        }

        return changed;
    }

    /// <summary>
    /// Moves everything unsupported down one cell. Rows are processed from the bottom up
    /// so that stacks fall together.
    /// </summary>
    private bool ApplyGravity()
    {
        bool changed = false;
        bool playerDone = false;
        for (int y = Level.Height - 2; y >= 0; y--)
        {
            if (!playerDone && Player.IsAlive && Player.Y == y)
            {
                changed |= DropPlayer();
                playerDone = true;
            }

            changed |= DropPieces(y);
            changed |= DropFires(y);
        }

        return changed;
    }

    private bool DropPlayer()
    {
        if (!IsPlayerFalling)
        {
            return false;
        }

        Player.Y++;
        if (Level[Player.X, Player.Y].Type == CellType.Fire)
        {
            Player.IsAlive = false;
        }

        return true;
    }

    private bool DropFires(int y)
    {
        bool changed = false;
        for (int x = 0; x < Level.Width; x++)
        {
            if (Level[x, y].Type != CellType.Fire)
            {
                continue;
            }

            if (IsPlayerAt(x, y + 1))
            {
                Player.IsAlive = false;
                changed = true;
            }
            else if (Level[x, y + 1].Type == CellType.Air)
            {
                Level[x, y + 1] = Cell.Fire;
                Level[x, y] = Cell.Air;
                moves.Add(new CellMove(x, y, x, y + 1));
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Ice at (x, y) and the ice or crate at (x + 1, y) are attached to each other. Attachment is
    /// recorded on the ice (crates have no flags of their own), so a crate is attached to ice
    /// beside it when that ice is attached toward the crate.
    /// </summary>
    private bool IsLinked(int x, int y)
    {
        Cell left = Level[x, y];
        Cell right = Level[x + 1, y];
        return (left.IsIce && left.AttachedRight && (right.IsIce ? right.AttachedLeft : right.Type == CellType.Crate))
            || (left.Type == CellType.Crate && right.IsIce && right.AttachedLeft);
    }

    /// <summary>A crate with ice attached to it is part of that ice piece and can't be pushed on its own.</summary>
    private bool IsAttachedCrate(int x, int y) =>
        Level[x, y].Type == CellType.Crate && (IsLinked(x - 1, y) || IsLinked(x, y));

    /// <summary>
    /// Finds each piece in row y (a run of ice cubes and crates attached to each other; a lone
    /// cube or crate is a piece of one) and drops it one cell if it isn't held by a fixed block
    /// and every cell under it is empty.
    /// </summary>
    private bool DropPieces(int y)
    {
        bool changed = false;
        int x = 0;
        while (x < Level.Width)
        {
            if (!Level[x, y].IsPushable && !Level[x, y].IsIce)
            {
                x++;
                continue;
            }

            int start = x;
            while (IsLinked(x, y))
            {
                x++;
            }

            int end = x;
            x++;
            changed |= TryDropPiece(start, end, y);
        }

        return changed;
    }

    private bool TryDropPiece(int start, int end, int y)
    {
        // Only fixed things (blocks, jars, pipes) hold a piece up from the side. A crate in the
        // piece holds it up only by resting on something, like any other member.
        bool heldByBlock =
            (Level[start, y].IsIce && Level[start, y].AttachedLeft && Level[start - 1, y].IsFixed)
            || (Level[end, y].IsIce && Level[end, y].AttachedRight && Level[end + 1, y].IsFixed);
        if (heldByBlock)
        {
            return false;
        }

        for (int x = start; x <= end; x++)
        {
            if (IsSliding(x, y) || !Level[x, y + 1].IsEmpty || IsPlayerAt(x, y + 1))
            {
                return false;
            }
        }

        List<int>? meltedAt = null;
        for (int x = start; x <= end; x++)
        {
            Cell thing = Level[x, y];
            Level[x, y] = Cell.Air;
            if (thing.IsIce && Level[x, y + 1].Type == CellType.Fire)
            {
                // Ice falls onto fire as if it were empty, then both disappear.
                Level[x, y + 1] = Cell.Air;
                (meltedAt ??= new List<int>()).Add(x);
            }
            else
            {
                // A crate landing on fire puts it out and takes its place.
                Level[x, y + 1] = thing;
                moves.Add(new CellMove(x, y, x, y + 1));
            }
        }

        if (meltedAt is not null)
        {
            foreach (int x in meltedAt)
            {
                DetachNeighbors(x, y + 1);
            }
        }

        return true;
    }

    /// <summary>
    /// Jars act on the cell directly on top of them: fire lights an unlit jar (the fire keeps
    /// burning), and a lit jar destroys ice and kills the player.
    /// </summary>
    private bool ApplyJars()
    {
        bool changed = false;
        for (int y = 1; y < Level.Height; y++)
        {
            for (int x = 0; x < Level.Width; x++)
            {
                CellType jar = Level[x, y].Type;
                Cell above = Level[x, y - 1];

                if (jar == CellType.UnlitJar && above.Type == CellType.Fire)
                {
                    Level[x, y] = Cell.LitJar;
                    changed = true;
                }
                else if (jar == CellType.LitJar && above.IsIce)
                {
                    RemoveIce(x, y - 1);
                    changed = true;
                }
                else if (jar == CellType.LitJar && IsPlayerAt(x, y - 1))
                {
                    Player.IsAlive = false;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void MovePlayer(int x, int y)
    {
        Player.X = x;
        Player.Y = y;
    }

    /// <summary>The player is standing at (x, y). A player inside a pipe is protected by it and doesn't count.</summary>
    private bool IsPlayerAt(int x, int y) => Player.IsAlive && !IsInPipe && Player.X == x && Player.Y == y;

    /// <summary>Something solid (block, ice or the player) is directly under (x, y).</summary>
    private bool IsSupported(int x, int y) => !Level[x, y + 1].IsEmpty || IsPlayerAt(x, y + 1);

    private void StopSliding(int x, int y) => sliding.RemoveAll(s => s.X == x && s.Y == y);

    private void UpdateStatus()
    {
        if (Status != GameStatus.Playing)
        {
            return;
        }

        if (!Player.IsAlive)
        {
            Status = GameStatus.Lost;
        }
        else if (startsWithFire && FiresLeft == 0)
        {
            // Real levels always have fires; a fireless test grid shouldn't count as won.
            Status = GameStatus.Won;
        }
    }

    private void OnChanged(ChangeKind kind)
    {
        LastChange = kind;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class PipeTravel
    {
        public PipeTravel(Heading moving)
        {
            Moving = moving;
        }

        /// <summary>The way the player was moving when they entered the current pipe cell.</summary>
        public Heading Moving { get; set; }

        /// <summary>The far end was blocked and the player is heading back out the entrance.</summary>
        public bool Returning { get; set; }

        public PipeTravel Clone() => new(Moving) { Returning = Returning };
    }

    /// <summary>
    /// One input that can be rewound: the game just before it, the cells the player has been
    /// through since (oldest first), and where each thing that moved since started out.
    /// </summary>
    private sealed class RewindPoint
    {
        public RewindPoint(GameState before)
        {
            Before = before;
        }

        public GameState Before { get; }

        public List<RewindStep> Trail { get; } = new();

        public Dictionary<(int X, int Y), (int X, int Y)> Origins { get; } = new();
    }

    /// <summary>A pushed ice cube or crate that is still moving sideways.</summary>
    private sealed class Slider
    {
        public Slider(int x, int y, Direction direction)
        {
            X = x;
            Y = y;
            Direction = direction;
        }

        public int X { get; set; }

        public int Y { get; }

        public Direction Direction { get; }

        /// <summary>How many cells it has moved since it was pushed.</summary>
        public int Moves { get; set; }

        public Slider Clone() => new(X, Y, Direction) { Moves = Moves };
    }
}
