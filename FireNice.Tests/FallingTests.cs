using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class FallingTests
{
    private static GameState StartInAir() => Game(
        "#####",
        "#.P.#",
        "#...#",
        "#...#",
        "#####");

    [Fact]
    public void PlayerStartingInAirFallsOneCellPerTick()
    {
        GameState game = StartInAir();
        Assert.True(game.IsPlayerFalling);

        Assert.True(game.Tick());
        Assert.Equal(2, game.Player.Y);
        Assert.True(game.Tick());
        Assert.Equal(3, game.Player.Y);
        Assert.False(game.Tick());

        Assert.Equal(3, game.Player.Y);
        Assert.False(game.IsPlayerFalling);
        Assert.True(game.Player.IsAlive);
    }

    [Fact]
    public void InputIsIgnoredWhileFalling()
    {
        GameState game = StartInAir();

        game.Press(Direction.Right);
        game.Press(Direction.Left);
        game.UseAction();

        Assert.Equal(2, game.Player.X);
        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Equal(CellType.Air, game.Level[3, 2].Type);
    }

    [Fact]
    public void WalkingOffALedgeFallsAndLandsSafely()
    {
        GameState game = Game(
            "######",
            "#P...#",
            "##...#",
            "#....#",
            "######");

        game.Press(Direction.Right);
        Assert.True(game.IsPlayerFalling);

        game.Press(Direction.Right); // Can't move sideways while falling.
        Assert.Equal(2, game.Player.X);

        RunUntilStill(game);
        Assert.Equal((2, 3), (game.Player.X, game.Player.Y));
        Assert.True(game.Player.IsAlive);

        game.Press(Direction.Right); // Input works again after landing.
        Assert.Equal(3, game.Player.X);
    }
}
