using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class ClimbTests
{
    [Fact]
    public void ClimbsOntoAOneHighBlock()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#P#.#",
            "#####");

        game.Press(Direction.Right);

        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void CannotClimbATwoHighWall()
    {
        GameState game = Game(
            "#####",
            "#.#.#",
            "#P#.#",
            "#####");

        game.Press(Direction.Right);

        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void CannotClimbWithACeilingAboveThePlayer()
    {
        GameState game = Game(
            "#####",
            "##..#",
            "#P#.#",
            "#####");

        game.Press(Direction.Right);

        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void ClimbsOntoIceThatCannotBePushed()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PI##",
            "#####");

        game.Press(Direction.Right);

        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
        Assert.True(game.Level[2, 2].IsFreeIce);
    }

    [Fact]
    public void ClimbsOntoAttachedIce()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P.>##",
            "######");

        game.Press(Direction.Right);
        RunUntilStill(game);
        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal((3, 1), (game.Player.X, game.Player.Y));
    }
}
