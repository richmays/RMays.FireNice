using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class IcePushTests
{
    [Fact]
    public void PushedIceSlidesToTheWallAndThePlayerStays()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI...#",
            "#######");

        game.Press(Direction.Right);
        Assert.Equal(1, game.Player.X);

        Assert.True(game.Tick());
        AssertRows(game,
            "#######",
            "#.....#",
            "#P.I..#",
            "#######");

        RunUntilStill(game);
        AssertRows(game,
            "#######",
            "#.....#",
            "#P...I#",
            "#######");
    }

    [Fact]
    public void SlidingIceStopsAtOtherIce()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI..I#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        AssertRows(game,
            "#######",
            "#.....#",
            "#P..II#",
            "#######");
    }

    [Fact]
    public void IcePushedOffALedgeFallsAndDoesNotSlideAgain()
    {
        GameState game = Game(
            "########",
            "#......#",
            "#PI....#",
            "###....#",
            "#......#",
            "########");

        game.Press(Direction.Right);
        RunUntilStill(game);

        AssertRows(game,
            "########",
            "#......#",
            "#P.....#",
            "###....#",
            "#..I...#",
            "########");
    }

    [Fact]
    public void TwoCubesSideBySideCannotBePushedSoThePlayerClimbs()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PII..#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        AssertRows(game,
            "#######",
            "#.P...#",
            "#.II..#",
            "#######");
    }

    [Fact]
    public void AttachedPieceCannotBePushed()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P><..#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        AssertRows(game,
            "#######",
            "#.P...#",
            "#.><..#",
            "#######");
    }

    [Fact]
    public void FireOnTopOfPushedIceFallsOffIt()
    {
        GameState game = Game(
            "#######",
            "#.F...#",
            "#PI...#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        AssertRows(game,
            "#######",
            "#.....#",
            "#PF..I#",
            "#######");
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void IceLandingOnThePlayerIsHarmlessAndFallsWhenThePlayerLeaves()
    {
        GameState game = Game(
            "#####",
            "#.I.#",
            "#.P.#",
            "#####");

        RunUntilStill(game);
        Assert.True(game.Player.IsAlive);
        Assert.True(game.Level[2, 1].IsIce);

        game.Press(Direction.Left);
        game.Press(Direction.Left);
        RunUntilStill(game);

        AssertRows(game,
            "#####",
            "#...#",
            "#PI.#",
            "#####");
    }
}
