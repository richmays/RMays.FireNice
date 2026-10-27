using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class PipeTests
{
    [Fact]
    public void WalkingIntoAnOpenEndEntersThePipeAndComesOutTheOtherSide()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P-..#",
            "######");

        game.Press(Direction.Right);
        Assert.True(game.IsInPipe);
        Assert.Equal(2, game.Player.X);

        Assert.True(game.Tick());
        Assert.False(game.IsInPipe);
        Assert.Equal((3, 2), (game.Player.X, game.Player.Y));
        Assert.Equal(Direction.Right, game.Player.Facing);
    }

    [Fact]
    public void ConnectedPipesAreFollowedOneCellPerTick()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P--..#",
            "#######");

        game.Press(Direction.Right);
        game.Tick();
        Assert.True(game.IsInPipe);
        Assert.Equal(3, game.Player.X);

        game.Tick();
        Assert.False(game.IsInPipe);
        Assert.Equal(4, game.Player.X);
    }

    [Fact]
    public void BlockedExitTurnsThePlayerAroundAndBringsThemBack()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P--##",
            "######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.False(game.IsInPipe);
        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
        Assert.Equal(Direction.Left, game.Player.Facing);
    }

    [Fact]
    public void PipeWithoutAnOpeningOnThatSideIsClimbedLikeABlock()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#P|.#",
            "#####");

        game.Press(Direction.Right);

        Assert.False(game.IsInPipe);
        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void UpEntersAPipeOpenAtTheBottom()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#|..#",
            "#P..#",
            "#####");

        game.PressUp();
        Assert.True(game.IsInPipe);
        RunUntilStill(game);

        Assert.False(game.IsInPipe);
        Assert.Equal((1, 1), (game.Player.X, game.Player.Y)); // Standing on top of the pipe.
    }

    [Fact]
    public void DownEntersAPipeOpenAtTheTopAndThePlayerFallsOutTheBottom()
    {
        GameState game = Game(
            "#####",
            "#P..#",
            "#|..#",
            "#...#",
            "#...#",
            "#####");

        game.PressDown();
        RunUntilStill(game);

        Assert.Equal((1, 4), (game.Player.X, game.Player.Y));
        Assert.True(game.Player.IsAlive);
    }

    [Fact]
    public void UpAndDownDoNothingWithoutAMatchingPipe()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#-..#",
            "#P..#",
            "#####");

        game.PressUp();
        game.PressDown();

        Assert.False(game.IsInPipe);
        Assert.Equal((1, 3), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void BendTurnsThePlayerUpward()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#P7.#",
            "#####");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void ChainOfBendsCarriesThePlayerOverAWall()
    {
        GameState game = Game(
            "#######",
            "#.3-1.#",
            "#.|#|.#",
            "#P7#9.#",
            "#######");

        game.Press(Direction.Left);  // Turn left...
        game.Press(Direction.Right); // ...and back, so we know facing changes come from the pipe.
        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.False(game.IsInPipe);
        Assert.Equal((5, 3), (game.Player.X, game.Player.Y));
        Assert.Equal(Direction.Right, game.Player.Facing);
    }

    [Fact]
    public void InputIsIgnoredWhileInAPipe()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P--..#",
            "#######");

        game.Press(Direction.Right);
        game.Press(Direction.Left);
        game.UseAction();
        game.PressUp();

        Assert.True(game.IsInPipe);
        Assert.Equal(Direction.Right, game.Player.Facing);
    }

    [Fact]
    public void PipesNeverFall()
    {
        string[] rows =
        {
            "#####",
            "#.|.#",
            "#...#",
            "#..P#",
            "#####",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void ComingOutIntoFireKillsThePlayer()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P-F.#",
            "######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void PlayerInsideAPipeIsSafeFromFallingFire()
    {
        GameState game = Game(
            "######",
            "#..F.#",
            "#P---#",
            "######");

        game.Press(Direction.Right);
        game.Tick(); // Player is now directly under the fire, inside the pipe.
        Assert.Equal((3, 2), (game.Player.X, game.Player.Y));
        RunUntilStill(game);

        Assert.True(game.Player.IsAlive);
        Assert.Equal((1, 2), (game.Player.X, game.Player.Y)); // Came back out the entrance.
    }

    [Fact]
    public void IceAttachesToPipesAndIsHeldUp()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "#-...#",
            "#....#",
            "######");

        game.UseAction();
        RunUntilStill(game);

        Assert.Equal("#-<..#", Rows(game)[3]);
    }

    [Fact]
    public void ResetTakesThePlayerOutOfThePipe()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P--..#",
            "#######");
        game.Press(Direction.Right);

        game.Reset();

        Assert.False(game.IsInPipe);
        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
    }
}
