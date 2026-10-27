using System.Text;
using FireNice.Core;

namespace FireNice.IO;

/// <summary>
/// Writes a level in the same format <see cref="LevelReader"/> reads.
/// </summary>
public static class LevelWriter
{
    public static string Write(LevelData level)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(level.Name))
        {
            text.Append("name: ").Append(level.Name).Append('\n');
        }

        text.Append("width: ").Append(level.Width).Append('\n');
        text.Append("height: ").Append(level.Height).Append('\n');
        text.Append("player: ").Append(level.PlayerStartX).Append(", ").Append(level.PlayerStartY).Append('\n');
        text.Append("grid:\n");
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                text.Append(CellChars.ToChar(level[x, y]));
            }

            text.Append('\n');
        }

        return text.ToString();
    }

    public static void Save(LevelData level, string path) => File.WriteAllText(path, Write(level));
}
