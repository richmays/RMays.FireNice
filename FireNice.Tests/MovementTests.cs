using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class MovementTests
{
    private static GameState OpenRoom() => Game(
        "#####",
        "#...#",
        "#.P.#",
        "#####");

    [Fact]
    public void PlayerStartsFacingRight()
    {
        Assert.Equal(Direction.Right, OpenRoom().Player.Facing);
    }

    [Fact]
    public void PressingTheOppositeDirectionOnlyTurns()
    {
        GameState game = OpenRoom();

        game.Press(Direction.Left);
        Assert.Equal(Direction.Left, game.Player.Facing);
        Assert.Equal(2, game.Player.X);

        game.Press(Direction.Left);
        Assert.Equal(1, game.Player.X);
    }

    [Fact]
    public void PressingTheFacingDirectionSteps()
    {
        GameState game = OpenRoom();

        game.Press(Direction.Right);

        Assert.Equal(3, game.Player.X);
        Assert.Equal(2, game.Player.Y);
    }

    [Fact]
    public void WallStopsThePlayer()
    {
        GameState game = Game(
            "#####",
            "##..#",
            "#P..#",
            "#####");

        game.Press(Direction.Left);
        game.Press(Direction.Left);

        Assert.Equal(1, game.Player.X);
        Assert.Equal(2, game.Player.Y);
    }
}
