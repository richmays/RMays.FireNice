using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class ResetTests
{
    [Fact]
    public void ResetRestoresTheOriginalLevel()
    {
        string[] rows =
        {
            "########",
            "#......#",
            "#.PI.F.#",
            "#.######",
            "########",
        };
        GameState game = Game(rows);
        string[] original = Rows(game);

        game.Press(Direction.Left);  // Turn left.
        game.UseAction();            // Create ice at (1, 3).
        game.Press(Direction.Right); // Turn right.
        game.Press(Direction.Right); // Push the ice into the fire.
        RunUntilStill(game);
        Assert.Equal(GameStatus.Won, game.Status);

        game.Reset();

        AssertRows(game, original);
        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Equal((2, 2), (game.Player.X, game.Player.Y));
        Assert.Equal(GameStatus.Playing, game.Status);
        Assert.True(game.Player.IsAlive);
    }

    [Fact]
    public void ResetAfterLosingStartsTheLevelOver()
    {
        GameState game = Game(
            "#####",
            "#.P.#",
            "#...#",
            "#.F.#",
            "#####");
        RunUntilStill(game);
        Assert.Equal(GameStatus.Lost, game.Status);

        game.Reset();

        Assert.Equal(GameStatus.Playing, game.Status);
        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
        Assert.True(game.IsPlayerFalling);
    }

    [Fact]
    public void ResetDoesNotChangeTheLevelData()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##...#",
            "#....#",
            "######");

        game.UseAction();
        game.Reset();

        Assert.Equal(CellType.Air, game.Data[2, 3].Type);
        Assert.Equal(CellType.Air, game.Level[2, 3].Type);
    }
}
