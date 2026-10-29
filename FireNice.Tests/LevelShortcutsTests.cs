using FireNice.Core;
using FireNice.IO;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class LevelShortcutsTests
{
    [Fact]
    public void Round10_6HasTheUsableCratesRule()
    {
        LevelData data = LevelReader.Load(Path.Combine(LevelsFolder(), "level_10-6.txt"));

        Assert.NotNull(LevelShortcuts.DeadEndRule(data));
    }

    [Theory]
    [InlineData("level01.txt")]
    [InlineData("level07.txt")]
    [InlineData("level_1-1.txt")]
    [InlineData("level_10-10.txt")]
    [InlineData("level_pipes.txt")]
    public void OtherLevelsHaveNoRule(string file)
    {
        LevelData data = LevelReader.Load(Path.Combine(LevelsFolder(), file));

        Assert.Null(LevelShortcuts.DeadEndRule(data));
    }

    [Fact]
    public void AChangedRound10_6HasNoRule()
    {
        // Same as Round 10-6 but with one crate swapped for ice: the rule may not hold any more.
        LevelData data = Data(
            "##########",
            "##.......#",
            "##P......#",
            "##ICICI..#",
            "##CICII..#",
            "#######..#",
            "#F.......#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.Null(LevelShortcuts.DeadEndRule(data));
    }

    [Fact]
    public void TheStartOfRound10_6IsNotADeadEnd()
    {
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), "level_10-6.txt")));

        Assert.False(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void CratesSideBySideOnTheShelfAreADeadEnd()
    {
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##I.ICI..#",
            "##CCIIC..#",
            "#######..#",
            "#F.......#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.True(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void CratesSideBySideOnIceAreNotADeadEnd()
    {
        // Removing the ice under one of them lets it fall, so the pair can still come apart.
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##I.CCI..#",
            "##CICIC..#",
            "#######..#",
            "#F.......#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.False(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void CratesSideBySideInThePitAreNotADeadEnd()
    {
        // The pit has to be filled with crates side by side, so stuck crates there are used, not lost.
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##I.I.I..#",
            "##CI.IC..#",
            "#######..#",
            "#F.......#",
            "##CCC.####",
            "##LLLL####",
            "##########");

        Assert.False(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void ACrateAgainstTheRightWallOfRow6IsADeadEnd()
    {
        // It can't be pushed left (the player would have to stand in the wall), so it's lost.
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##ICICI..#",
            "##CICI...#",
            "#######..#",
            "#F......C#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.True(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void CratesSideBySideOnRow6AreADeadEnd()
    {
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##ICICI..#",
            "##CI.I...#",
            "#######..#",
            "#F....CC.#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.True(LevelShortcuts.TooFewUsableCrates(game));
    }

    [Fact]
    public void ALoneCrateOnRow6IsNotADeadEnd()
    {
        GameState game = Game(
            "##########",
            "##.......#",
            "##P......#",
            "##ICICI..#",
            "##CIC.I..#",
            "#######..#",
            "#F....C..#",
            "##....####",
            "##LLLL####",
            "##########");

        Assert.False(LevelShortcuts.TooFewUsableCrates(game));
    }
}
