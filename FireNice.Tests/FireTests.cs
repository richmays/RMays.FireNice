using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class FireTests
{
    private static GameState PushIntoFire() => Game(
        "#######",
        "#.....#",
        "#PI.F.#",
        "#######");

    [Fact]
    public void IcePushedIntoFireDestroysBothAndWins()
    {
        GameState game = PushIntoFire();

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P....#", Rows(game)[2]);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void IceFallingOntoFireDestroysBoth()
    {
        GameState game = Game(
            "#####",
            "#.I.#",
            "#...#",
            "#.FP#",
            "#####");

        RunUntilStill(game);

        Assert.Equal(0, game.FiresLeft);
        Assert.False(game.Level[2, 3].IsIce);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void FireFallingOntoIceSurvives()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#...#",
            "#.IP#",
            "#####");

        RunUntilStill(game);

        AssertRows(game,
            "#####",
            "#...#",
            "#.F.#",
            "#.IP#",
            "#####");
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void StandingNextToFireIsSafe()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PF.#",
            "#####");

        RunUntilStill(game);

        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void WalkingIntoFireLoses()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PF.#",
            "#####");

        game.Press(Direction.Right);

        Assert.False(game.Player.IsAlive);
        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void FallingOntoFireLoses()
    {
        GameState game = Game(
            "#####",
            "#.P.#",
            "#...#",
            "#.F.#",
            "#####");

        RunUntilStill(game);

        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void FireFallingOntoThePlayerLoses()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#...#",
            "#.P.#",
            "#####");

        RunUntilStill(game);

        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void NothingHappensAfterWinning()
    {
        GameState game = PushIntoFire();
        game.Press(Direction.Right);
        RunUntilStill(game);
        string[] before = Rows(game);

        game.Press(Direction.Right);
        game.Press(Direction.Right);

        AssertRows(game, before);
        Assert.False(game.Tick());
    }

    [Fact]
    public void NothingHappensAfterLosing()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#.PF.#",
            "######");
        game.Press(Direction.Right);
        Assert.Equal(GameStatus.Lost, game.Status);

        game.Press(Direction.Left);

        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.False(game.Tick());
    }
}
