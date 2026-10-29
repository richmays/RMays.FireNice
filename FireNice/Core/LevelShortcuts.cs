namespace FireNice.Core;

/// <summary>
/// Dead-end rules that only hold for one particular level. The solver uses them to stop searching
/// positions that can't lead to a win. A rule is only used for the exact level it was worked out
/// for (same grid and start), because in another level the same pattern may be part of the answer.
/// </summary>
public static class LevelShortcuts
{
    /// <summary>Round 10-6, exactly as shipped in Levels/level_10-6.txt.</summary>
    private static readonly string[] Round10_6Grid =
    {
        "##########",
        "##.......#",
        "##.......#",
        "##ICICI..#",
        "##CICIC..#",
        "#######..#",
        "#F.......#",
        "##....####",
        "##LLLL####",
        "##########",
    };

    /// <summary>
    /// Round 10-6 can only be won by sliding something along row 6 into the fire, and that needs all
    /// four pit cells over the lit jars filled first. Only crates can fill them (ice melts on a lit
    /// jar), so the level needs four crates in the pit.
    /// </summary>
    private const int Round10_6CratesNeeded = 4;

    /// <summary>The dead-end rule for this level, or null if there isn't one.</summary>
    public static Func<GameState, bool>? DeadEndRule(LevelData data) =>
        IsRound10_6(data) ? TooFewUsableCrates : null;

    /// <summary>
    /// Round 10-6: true when fewer than four crates are either already in the pit (resting on a lit
    /// jar) or still able to move, so the pit can never be filled.
    /// </summary>
    internal static bool TooFewUsableCrates(GameState game)
    {
        // The solver calls this for every position, so it works in a stack buffer, not a new array.
        Level level = game.Level;
        Span<bool> stuck = stackalloc bool[level.Width * level.Height];
        MarkStuckCrates(level, stuck);
        int crates = 0;
        int stuckOutsidePit = 0;
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (level[x, y].Type != CellType.Crate)
                {
                    continue;
                }

                crates++;
                if (stuck[(y * level.Width) + x] && level[x, y + 1].Type != CellType.LitJar)
                {
                    stuckOutsidePit++;
                }
            }
        }

        return crates - stuckOutsidePit < Round10_6CratesNeeded;
    }

    /// <summary>
    /// Crates that can never move again. A crate resting on something fixed never falls. Pushing it
    /// either way needs the player on one side and an empty cell on the other, so it can never be
    /// pushed once either side is permanently filled: by something fixed, or by another crate that
    /// can never move. Starting from every crate on something fixed, this keeps dropping the ones
    /// with neither side permanently filled until none are left to drop; what remains is stuck.
    /// (Crates are never destroyed, and the player can't stand in a crate or a block.)
    /// </summary>
    private static void MarkStuckCrates(Level level, Span<bool> stuck)
    {
        int width = level.Width;
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                stuck[(y * width) + x] = level[x, y].Type == CellType.Crate && level[x, y + 1].IsFixed;
            }
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width) + x;
                    if (stuck[i] && !IsFilled(level, stuck, x - 1, y) && !IsFilled(level, stuck, x + 1, y))
                    {
                        stuck[i] = false;
                        changed = true;
                    }
                }
            }
        }
    }

    /// <summary>The cell is permanently filled: something fixed (or off the grid), or a stuck crate.</summary>
    private static bool IsFilled(Level level, ReadOnlySpan<bool> stuck, int x, int y) =>
        level[x, y].IsFixed || (level.InBounds(x, y) && stuck[(y * level.Width) + x]);

    private static bool IsRound10_6(LevelData data)
    {
        if (data.Width != Round10_6Grid[0].Length || data.Height != Round10_6Grid.Length
            || data.PlayerStartX != 2 || data.PlayerStartY != 2)
        {
            return false;
        }

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                if (CellChars.ToChar(data[x, y]) != Round10_6Grid[y][x])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
