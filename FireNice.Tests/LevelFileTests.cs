using FireNice.Core;
using FireNice.IO;

namespace FireNice.Tests;

public class LevelFileTests
{
    private const string Valid =
        "; A comment\n" +
        "name: Sample\n" +
        "width: 6\n" +
        "height: 4\n" +
        "player: 1, 2\n" +
        "grid:\n" +
        "######\n" +
        "#..F.#\n" +
        "#.I>=#\n" +
        "######\n";

    [Fact]
    public void ReadsTheHeaderAndGrid()
    {
        LevelData level = LevelReader.Parse(Valid);

        Assert.Equal("Sample", level.Name);
        Assert.Equal(6, level.Width);
        Assert.Equal(4, level.Height);
        Assert.Equal((1, 2), (level.PlayerStartX, level.PlayerStartY));
        Assert.Equal(CellType.Fire, level[3, 1].Type);
        Assert.Equal(Cell.Ice(), level[2, 2]);
        Assert.Equal(Cell.Ice(attachedRight: true), level[3, 2]);
        Assert.Equal(Cell.Ice(attachedLeft: true, attachedRight: true), level[4, 2]);
    }

    [Fact]
    public void ReadsAllSixPipes()
    {
        LevelData level = LevelReader.Parse(Valid
            .Replace("#.I>=#", "#|-79#")
            .Replace("#..F.#", "#13F.#")
            .Replace("player: 1, 2", "player: 4, 1"));

        Assert.Equal(Openings.Up | Openings.Down, level[1, 2].PipeOpenings);
        Assert.Equal(Openings.Left | Openings.Right, level[2, 2].PipeOpenings);
        Assert.Equal(Openings.Up | Openings.Left, level[3, 2].PipeOpenings);
        Assert.Equal(Openings.Up | Openings.Right, level[4, 2].PipeOpenings);
        Assert.Equal(Openings.Down | Openings.Left, level[1, 1].PipeOpenings);
        Assert.Equal(Openings.Down | Openings.Right, level[2, 1].PipeOpenings);
    }

    [Fact]
    public void ReadsCrates()
    {
        LevelData level = LevelReader.Parse(Valid.Replace("#.I>=#", "#.C..#"));

        Assert.Equal(CellType.Crate, level[2, 2].Type);
    }

    [Fact]
    public void ReadsIceAttachedToACrate()
    {
        LevelData level = LevelReader.Parse(Valid.Replace("#.I>=#", "#.C<.#"));

        Assert.Equal(Cell.Ice(attachedLeft: true), level[3, 2]);
    }

    [Fact]
    public void ReadsJarsIncludingInTheBorderAndAsAnAttachmentPoint()
    {
        const string text =
            "name: Jars\n" +
            "width: 6\n" +
            "height: 4\n" +
            "player: 1, 1\n" +
            "grid:\n" +
            "######\n" +
            "#..F.#\n" +
            "#J<.L#\n" +
            "##J#L#\n";

        LevelData level = LevelReader.Parse(text);

        Assert.Equal(CellType.UnlitJar, level[1, 2].Type);
        Assert.Equal(CellType.LitJar, level[4, 2].Type);
        Assert.Equal(CellType.UnlitJar, level[2, 3].Type);
        Assert.Equal(CellType.LitJar, level[4, 3].Type);
        Assert.Equal(text, LevelWriter.Write(level));
    }

    [Fact]
    public void AcceptsWindowsLineEndings()
    {
        LevelData level = LevelReader.Parse(Valid.Replace("\n", "\r\n"));

        Assert.Equal(6, level.Width);
    }

    [Fact]
    public void WritingWhatWasReadGivesTheSameText()
    {
        string text = Valid.Replace("; A comment\n", string.Empty);

        Assert.Equal(text, LevelWriter.Write(LevelReader.Parse(text)));
    }

    public static IEnumerable<object[]> BrokenLevels() => new[]
    {
        new object[] { "missing width", Valid.Replace("width: 6\n", string.Empty) },
        new object[] { "missing player", Valid.Replace("player: 1, 2\n", string.Empty) },
        new object[] { "missing grid line", Valid.Replace("grid:\n", string.Empty) },
        new object[] { "width not a number", Valid.Replace("width: 6", "width: six") },
        new object[] { "unknown setting", Valid.Replace("name: Sample\n", "name: Sample\nspeed: 3\n") },
        new object[] { "too few rows", Valid.Replace("height: 4", "height: 5") },
        new object[] { "too many rows", Valid + "######\n" },
        new object[] { "row wrong length", Valid.Replace("width: 6", "width: 7") },
        new object[] { "unknown character", Valid.Replace("#..F.#", "#..X.#") },
        new object[] { "gap in border", Valid.Replace("######\n#..F.#", "##.###\n#..F.#") },
        new object[] { "player on a block", Valid.Replace("player: 1, 2", "player: 0, 0") },
        new object[] { "player outside grid", Valid.Replace("player: 1, 2", "player: 9, 9") },
        new object[] { "no fires", Valid.Replace("#..F.#", "#....#") },
        new object[] { "attached to air", Valid.Replace("#.I>=#", "#..<.#") },
        new object[] { "attachment not matched", Valid.Replace("#.I>=#", "#.I=##") },
        new object[] { "crate in the border", Valid.Replace("######\n#..F.#", "##C###\n#..F.#") },
    };

    [Theory]
    [MemberData(nameof(BrokenLevels))]
    public void RejectsBrokenLevels(string problem, string text)
    {
        var error = Assert.Throws<LevelFormatException>(() => LevelReader.Parse(text));

        Assert.False(string.IsNullOrWhiteSpace(error.Message), problem);
    }

    [Fact]
    public void ErrorsSayWhichLineIsWrong()
    {
        var error = Assert.Throws<LevelFormatException>(() => LevelReader.Parse(Valid.Replace("#..F.#", "#..X.#")));

        Assert.StartsWith("Line 8:", error.Message);
    }

    [Fact]
    public void EveryShippedLevelLoads()
    {
        string[] files = Directory.GetFiles(TestLevel.LevelsFolder(), "*.txt");
        Assert.NotEmpty(files);

        foreach (string file in files)
        {
            LevelData level = LevelReader.Load(file);
            Assert.True(level.Width > 0, file);
        }
    }
}
