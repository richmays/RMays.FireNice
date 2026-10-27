using FireNice.Core;

namespace FireNice.IO;

/// <summary>
/// Reads the human-readable level format:
/// <code>
/// ; comment
/// name: Bridge Practice
/// width: 12
/// height: 6
/// player: 1, 2
/// grid:
/// ############
/// ...
/// </code>
/// </summary>
public static class LevelReader
{
    public static LevelData Load(string path) =>
        Parse(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));

    public static LevelData Parse(string text, string defaultName = "Untitled", bool requireFire = true)
    {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        string name = defaultName;
        int? width = null;
        int? height = null;
        (int X, int Y)? player = null;
        int gridStart = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.Equals("grid:", StringComparison.OrdinalIgnoreCase))
            {
                gridStart = i + 1;
                break;
            }

            int colon = line.IndexOf(':');
            if (colon < 0)
            {
                throw Error(i, $"Expected \"key: value\" but found \"{line}\".");
            }

            string key = line[..colon].Trim().ToLowerInvariant();
            string value = line[(colon + 1)..].Trim();
            switch (key)
            {
                case "name":
                    name = value;
                    break;
                case "width":
                    width = ParseNumber(value, i, "width");
                    break;
                case "height":
                    height = ParseNumber(value, i, "height");
                    break;
                case "player":
                    player = ParsePosition(value, i);
                    break;
                default:
                    throw Error(i, $"Unknown setting \"{key}\".");
            }
        }

        if (width is null)
        {
            throw new LevelFormatException("Missing \"width:\" line.");
        }

        if (height is null)
        {
            throw new LevelFormatException("Missing \"height:\" line.");
        }

        if (player is null)
        {
            throw new LevelFormatException("Missing \"player:\" line.");
        }

        if (gridStart < 0)
        {
            throw new LevelFormatException("Missing \"grid:\" line.");
        }

        int w = width.Value;
        int h = height.Value;
        if (w < 3 || h < 3)
        {
            throw new LevelFormatException($"The grid must be at least 3 x 3 (got {w} x {h}).");
        }

        var cells = new Cell[w, h];
        for (int y = 0; y < h; y++)
        {
            int lineIndex = gridStart + y;
            if (lineIndex >= lines.Length)
            {
                throw Error(lineIndex, $"The grid has only {y} rows, but height is {h}.");
            }

            string row = lines[lineIndex].TrimEnd();
            if (row.Length != w)
            {
                throw Error(lineIndex, $"Grid row {y} has {row.Length} characters, but width is {w}.");
            }

            for (int x = 0; x < w; x++)
            {
                cells[x, y] = CellChars.FromChar(row[x])
                    ?? throw Error(lineIndex, $"Unknown character '{row[x]}' in column {x}.");
            }
        }

        for (int i = gridStart + h; i < lines.Length; i++)
        {
            string extra = lines[i].Trim();
            if (extra.Length > 0 && !extra.StartsWith(';'))
            {
                throw Error(i, $"The grid has more than {h} rows (height is {h}).");
            }
        }

        Validate(cells, player.Value, gridStart, requireFire);
        return new LevelData(name, cells, player.Value.X, player.Value.Y);
    }

    private static void Validate(Cell[,] cells, (int X, int Y) player, int gridStart, bool requireFire)
    {
        int w = cells.GetLength(0);
        int h = cells.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool onBorder = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                if (onBorder && !cells[x, y].IsFixed)
                {
                    throw Error(gridStart + y, $"The outer border must be solid ('#' or a jar), but column {x} is '{CellChars.ToChar(cells[x, y])}'.");
                }
            }
        }

        if (player.X < 0 || player.Y < 0 || player.X >= w || player.Y >= h)
        {
            throw new LevelFormatException($"The player's start ({player.X}, {player.Y}) is outside the grid.");
        }

        if (cells[player.X, player.Y].Type != CellType.Air)
        {
            throw Error(gridStart + player.Y, $"The player's start ({player.X}, {player.Y}) must be an air cell '.'.");
        }

        if (requireFire && !cells.Cast<Cell>().Any(c => c.Type == CellType.Fire))
        {
            throw new LevelFormatException("The level has no fires ('F').");
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Cell cell = cells[x, y];
                if (!cell.IsIce)
                {
                    continue;
                }

                if (cell.AttachedLeft)
                {
                    CheckAttachment(cells[x - 1, y], neighborAttachedBack: cells[x - 1, y].AttachedRight, x, y, "left", gridStart);
                }

                if (cell.AttachedRight)
                {
                    CheckAttachment(cells[x + 1, y], neighborAttachedBack: cells[x + 1, y].AttachedLeft, x, y, "right", gridStart);
                }
            }
        }
    }

    private static void CheckAttachment(Cell neighbor, bool neighborAttachedBack, int x, int y, string side, int gridStart)
    {
        if (neighbor.IsAnchor)
        {
            return;
        }

        if (!neighbor.IsIce)
        {
            throw Error(gridStart + y, $"Ice in column {x} is attached on its {side}, but there's no block, jar, pipe, crate or ice there.");
        }

        if (!neighborAttachedBack)
        {
            throw Error(gridStart + y, $"Ice in column {x} is attached on its {side}, but the ice there isn't attached back.");
        }
    }

    private static int ParseNumber(string value, int lineIndex, string key)
    {
        if (!int.TryParse(value, out int number))
        {
            throw Error(lineIndex, $"\"{key}\" must be a whole number, but was \"{value}\".");
        }

        return number;
    }

    private static (int X, int Y) ParsePosition(string value, int lineIndex)
    {
        string[] parts = value.Split(',');
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim(), out int x)
            || !int.TryParse(parts[1].Trim(), out int y))
        {
            throw Error(lineIndex, $"\"player\" must be \"x, y\", but was \"{value}\".");
        }

        return (x, y);
    }

    private static LevelFormatException Error(int lineIndex, string message) =>
        new($"Line {lineIndex + 1}: {message}");
}
