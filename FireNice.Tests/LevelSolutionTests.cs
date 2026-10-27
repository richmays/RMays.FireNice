using FireNice.Core;
using FireNice.IO;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

/// <summary>
/// Plays a known solution for each shipped level, to make sure every level can be won.
/// </summary>
public class LevelSolutionTests
{
    [Theory]
    [InlineData("level01.txt", new[] { "R", "R", "R" })]
    [InlineData("level02.txt", new[] { "R", "R" })]
    [InlineData("level03.txt", new[] { "R", "R", "A", "R", "A", "R", "A", "R", "R", "R" })]
    [InlineData("level04.txt", new[] { "A", "R", "R", "R", "R", "R" })]
    [InlineData("level05.txt", new[] { "R", "R", "A", "R", "A", "R", "A", "R", "R", "R", "R" })]
    [InlineData("level06.txt", new[] { "R", "R", "R", "R" })]
    [InlineData("level07.txt", new[] { "R", "R", "R", "U", "R" })]
    public void LevelCanBeWon(string file, string[] inputs)
    {
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), file)));

        Play(game, inputs);

        Assert.Equal(GameStatus.Won, game.Status);
    }
}
