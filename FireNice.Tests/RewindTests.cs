using System.Drawing;
using FireNice.Core;
using FireNice.UI;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class RewindTests
{
    [Fact]
    public void RewindAfterFallingOffALedgePutsThePlayerBackOnTheLedgeFacingTheSameWay()
    {
        GameState game = Game(
            "######",
            "#...P#",
            "#...##",
            "#....#",
            "######");
        Play(game, "L", "L"); // Turn left, then step off the ledge and fall.
        Assert.Equal((3, 3), (game.Player.X, game.Player.Y));

        Assert.True(game.Rewind());

        Assert.Equal((4, 1), (game.Player.X, game.Player.Y));
        Assert.Equal(Direction.Left, game.Player.Facing);
        Assert.Equal(ChangeKind.Rewind, game.LastChange);
        Assert.False(game.IsPlayerFalling);
    }

    [Fact]
    public void EachRewindUndoesTheInputBeforeIt()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        Play(game, "R", "R", "L");
        Assert.Equal(3, game.RewindCount);

        Assert.True(game.Rewind());
        Assert.Equal((3, 2, Direction.Right), (game.Player.X, game.Player.Y, game.Player.Facing));

        Assert.True(game.Rewind());
        Assert.Equal((2, 2, Direction.Right), (game.Player.X, game.Player.Y, game.Player.Facing));

        Assert.True(game.Rewind());
        Assert.Equal((1, 2, Direction.Right), (game.Player.X, game.Player.Y, game.Player.Facing));

        Assert.False(game.CanRewind);
        Assert.False(game.Rewind());
        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void RewindingEveryInputLeadsBackToTheStartOfTheLevel()
    {
        GameState game = Game(
            "#########",
            "#.......#",
            "#.PI.F..#",
            "#######.#",
            "#F......#",
            "#########");
        RunUntilStill(game);
        string[] start = Rows(game);

        // Push the ice into the top fire, walk to the hole, fill it with ice and clear it again,
        // then drop through it and walk left along the bottom.
        Play(game, "R", "R", "R", "R", "R", "A", "A", "R", "L", "L", "L");
        AssertRows(
            game,
            "#########",
            "#.......#",
            "#.......#",
            "#######.#",
            "#F...P..#",
            "#########");
        Assert.Equal(11, game.RewindCount);

        while (game.Rewind())
        {
            RunUntilStill(game);
        }

        AssertRows(game, start);
        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void PlayingOnAfterARewindKeepsTheEarlierSteps()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        Play(game, "R", "R");
        game.Rewind();
        Play(game, "L");

        game.Rewind(); // Undoes the turn left.
        Assert.Equal((2, 2, Direction.Right), (game.Player.X, game.Player.Y, game.Player.Facing));

        game.Rewind(); // Undoes the first step.
        Assert.Equal((1, 2), (game.Player.X, game.Player.Y));
        Assert.False(game.CanRewind);
    }

    [Fact]
    public void RewindUndoesATurn()
    {
        GameState game = Game(
            "#####",
            "#.P.#",
            "#####");

        game.Press(Direction.Left);
        game.Rewind();

        Assert.Equal(Direction.Right, game.Player.Facing);
        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void InputsThatChangeNothingDoNotReplaceTheRewindStep()
    {
        GameState game = Game(
            "#####",
            "#.P##",
            "#####");

        game.Press(Direction.Left);  // Turn left.
        game.Press(Direction.Right); // Turn right.
        game.Press(Direction.Right); // Walk into the wall; nothing happens.
        game.UseAction();            // The target is a block; nothing happens.
        game.Rewind();

        Assert.Equal(Direction.Left, game.Player.Facing);
    }

    [Fact]
    public void RewindUndoesDying()
    {
        GameState game = Game(
            "#####",
            "#PF.#",
            "#####");
        game.Press(Direction.Right);
        Assert.Equal(GameStatus.Lost, game.Status);

        game.Rewind();

        Assert.Equal(GameStatus.Playing, game.Status);
        Assert.True(game.Player.IsAlive);
        Assert.Equal((1, 1), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void RewindUndoesAPushAndEverythingItSetOff()
    {
        GameState game = Game(
            "########",
            "#......#",
            "#.PI.F.#",
            "#.######",
            "########");
        string[] beforePush = Rows(game);
        game.Press(Direction.Right); // Push the ice; it slides into the fire and both are gone.
        RunUntilStill(game);
        Assert.Equal(GameStatus.Won, game.Status);

        game.Rewind();

        AssertRows(game, beforePush);
        Assert.Equal(GameStatus.Playing, game.Status);
        Assert.Contains(new CellMove(4, 2, 3, 2), game.LastMoves); // The ice glides back from where it melted.
    }

    [Fact]
    public void RewindUndoesIceAndWhatFellOffIt()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##...#",
            "#....#",
            "######");
        game.UseAction(); // Ice at (2, 3), held by the block to its left.
        game.Rewind();

        Assert.Equal(CellType.Air, game.Level[2, 3].Type);
    }

    [Fact]
    public void RewindPathRetracesThePlayersCells()
    {
        GameState game = Game(
            "######",
            "#P...#",
            "##...#",
            "#....#",
            "######");
        Play(game, "R");

        game.Rewind();

        Assert.Equal(
            new[]
            {
                new RewindStep(2, 3, ChangeKind.Tick),
                new RewindStep(2, 2, ChangeKind.Tick),
                new RewindStep(2, 1, ChangeKind.Input),
                new RewindStep(1, 1, ChangeKind.Input),
            },
            game.LastRewindPath);
    }

    [Fact]
    public void ResetForgetsTheRewindStep()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        game.Press(Direction.Right);

        game.Reset();

        Assert.False(game.CanRewind);
    }

    [Fact]
    public void CopiesDoNotKeepARewindStep()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        game.Press(Direction.Right);

        GameState copy = game.Clone();
        copy.Press(Direction.Right);

        Assert.True(game.CanRewind);
        Assert.False(copy.KeepsRewind);
        Assert.False(copy.CanRewind);
    }

    [Fact]
    public void RewindAnimationRetracesThePathAtDoubleSpeed()
    {
        GameState game = Game(
            "######",
            "#P...#",
            "##...#",
            "#....#",
            "######");
        var animator = new Animator(160, 100);
        animator.Sync(game, 0);
        game.Press(Direction.Right); // Step off the ledge (160 ms), then fall two cells (100 ms each).
        animator.Sync(game, 0);
        RunUntilStill(game);
        animator.Sync(game, 1000);

        game.Rewind();
        animator.Sync(game, 2000);

        Assert.Equal(new PointF(2, 3), animator.PlayerPosition(2000));
        Assert.Equal(new PointF(2, 2), animator.PlayerPosition(2050)); // Each fall cell takes 50 ms.
        Assert.Equal(new PointF(2, 1), animator.PlayerPosition(2100));
        Assert.Equal(new PointF(1.5f, 1), animator.PlayerPosition(2140)); // The step takes 80 ms.
        Assert.True(animator.IsAnimating(2179));
        Assert.Equal(new PointF(1, 1), animator.PlayerPosition(2180));
        Assert.False(animator.IsAnimating(2180));
    }

    [Fact]
    public void RewoundThingsGlideBackAtDoubleSpeed()
    {
        GameState game = Game(
            "########",
            "#......#",
            "#.PI...#",
            "#.######",
            "########");
        game.Press(Direction.Right); // The ice slides three cells to the wall.
        RunUntilStill(game);
        var animator = new Animator(150, 100);
        animator.Sync(game, 0);

        game.Rewind();
        animator.Sync(game, 1000);

        Assert.Equal(new PointF(6, 2), animator.CellPosition(3, 2, 1000));
        Assert.Equal(new PointF(4.5f, 2), animator.CellPosition(3, 2, 1075));
        Assert.Equal(new PointF(3, 2), animator.CellPosition(3, 2, 1150));
    }
}
