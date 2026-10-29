using FireNice.Core;
using FireNice.IO;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

/// <summary>Keys are ignored until everything has stopped moving.</summary>
public class SettleBeforeInputTests
{
    [Fact]
    public void QuickSecondPushInRound1_1DoesNotClimbOntoTheFallingIce()
    {
        // The bug: in Round 1-1, push the bottom of a two-high stack of ice, then press Right
        // again one tick later. The top cube has just dropped into place and the player climbed it.
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), "level_1-1.txt")));
        RunUntilStill(game);
        for (int i = 0; i < 8; i++)
        {
            game.Press(Direction.Left); // Over to the left wall, then down the shaft beside the stack.
            RunUntilStill(game);
        }

        game.Press(Direction.Right); // Turn to face the stack.
        for (int push = 0; push < 2; push++)
        {
            game.Press(Direction.Right);
            RunUntilStill(game);
        }

        Assert.Equal("#PI.F....#", Rows(game)[7]);
        Assert.Equal('I', Rows(game)[6][2]); // Two cubes high now.

        game.Press(Direction.Right); // Push the bottom one.
        game.Tick();                 // The top one drops into its place...
        game.Press(Direction.Right); // ...and this quick press must be ignored.
        RunUntilStill(game);

        Assert.Equal((1, 7), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void KeysAreIgnoredWhileIceIsSliding()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.PI.....#",
            "##########");
        game.Press(Direction.Right); // Push: the ice starts sliding.

        Assert.False(game.CanAct);
        game.Press(Direction.Left);
        Assert.Equal(Direction.Right, game.Player.Facing);

        RunUntilStill(game);
        Assert.True(game.CanAct);
    }

    [Fact]
    public void KeysAreIgnoredWhileSomethingElseIsFalling()
    {
        GameState game = Game(
            "#######",
            "#...I.#",
            "#.....#",
            "#P....#",
            "#######");

        Assert.False(game.CanAct); // The ice falls from the start.
        game.Press(Direction.Right);
        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Equal(1, game.Player.X);

        RunUntilStill(game);
        game.Press(Direction.Right);
        Assert.Equal(2, game.Player.X);
    }

    [Fact]
    public void KeysWorkRightAwayWhenNothingMoves()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");

        game.Press(Direction.Right);
        game.Press(Direction.Right); // No tick needed in between: nothing was moving.

        Assert.Equal(3, game.Player.X);
    }

    [Fact]
    public void KeysWorkAgainAsSoonAsTheLastThingLands()
    {
        GameState game = Game(
            "#######",
            "#.I...#",
            "#P....#",
            "#######");

        game.Tick(); // The ice lands beside the player; that tick moved something.

        Assert.True(game.CanAct); // But nothing else will, so keys work at once.
    }
}
