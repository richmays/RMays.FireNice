using FireNice.Core;
using FireNice.IO;

namespace FireNice.Tests;

/// <summary>
/// Builds small test levels from text rows. Rows use the level file alphabet,
/// plus 'P' to mark where the player starts.
/// </summary>
internal static class TestLevel
{
    public static LevelData Data(params string[] rows)
    {
        int playerX = -1;
        int playerY = -1;
        var grid = new string[rows.Length];
        for (int y = 0; y < rows.Length; y++)
        {
            int x = rows[y].IndexOf('P');
            if (x >= 0)
            {
                playerX = x;
                playerY = y;
            }

            grid[y] = rows[y].Replace('P', '.');
        }

        if (playerX < 0)
        {
            throw new ArgumentException("Test rows need a 'P' for the player.");
        }

        string text = $"width: {rows[0].Length}\nheight: {rows.Length}\nplayer: {playerX}, {playerY}\ngrid:\n"
            + string.Join("\n", grid) + "\n";
        return LevelReader.Parse(text, "Test", requireFire: false);
    }

    public static GameState Game(params string[] rows) => new(Data(rows));

    /// <summary>Ticks until nothing moves. Returns how many ticks moved something.</summary>
    public static int RunUntilStill(GameState game, int maxTicks = 500)
    {
        int ticks = 0;
        while (game.Tick())
        {
            if (++ticks > maxTicks)
            {
                throw new InvalidOperationException("The level never settled.");
            }
        }

        return ticks;
    }

    /// <summary>The grid as text rows, with 'P' where the (living) player is.</summary>
    public static string[] Rows(GameState game)
    {
        var rows = new string[game.Level.Height];
        for (int y = 0; y < game.Level.Height; y++)
        {
            var row = new char[game.Level.Width];
            for (int x = 0; x < game.Level.Width; x++)
            {
                row[x] = CellChars.ToChar(game.Level[x, y]);
            }

            if (game.Player.IsAlive && game.Player.Y == y)
            {
                row[game.Player.X] = 'P';
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    public static void AssertRows(GameState game, params string[] expected) => Assert.Equal(expected, Rows(game));

    /// <summary>
    /// Plays a sequence of inputs, letting everything settle after each one.
    /// "L" / "R" / "U" / "D" press a direction, "A" uses the action button.
    /// </summary>
    public static void Play(GameState game, params string[] inputs)
    {
        RunUntilStill(game);
        foreach (string input in inputs)
        {
            switch (input)
            {
                case "L":
                    game.Press(Direction.Left);
                    break;
                case "R":
                    game.Press(Direction.Right);
                    break;
                case "U":
                    game.PressUp();
                    break;
                case "D":
                    game.PressDown();
                    break;
                case "A":
                    game.UseAction();
                    break;
                default:
                    throw new ArgumentException($"Unknown input \"{input}\".");
            }

            RunUntilStill(game);
        }
    }

    public static string LevelsFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FireNice.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new DirectoryNotFoundException("Couldn't find FireNice.sln above the test output folder.");
        }

        return Path.Combine(dir.FullName, "FireNice", "Levels");
    }
}
