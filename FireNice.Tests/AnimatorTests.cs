using System.Drawing;
using FireNice.Core;
using FireNice.UI;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class AnimatorTests
{
    private const int Walk = 150;
    private const int TickLength = 100;

    private static (GameState Game, Animator Animator) Start(params string[] rows)
    {
        GameState game = Game(rows);
        var animator = new Animator(Walk, TickLength);
        animator.Sync(game, 0);
        return (game, animator);
    }

    [Fact]
    public void StepGlidesBetweenCells()
    {
        var (game, animator) = Start(
            "#######",
            "#.....#",
            "#P....#",
            "#######");

        game.Press(Direction.Right);
        animator.Sync(game, 0);

        Assert.Equal(new PointF(1, 2), animator.PlayerPosition(0));
        Assert.Equal(new PointF(1.5f, 2), animator.PlayerPosition(75));
        Assert.Equal(new PointF(2, 2), animator.PlayerPosition(150));
        Assert.False(animator.IsAnimating(150));
    }

    [Fact]
    public void QuickSecondStepSkipsTheFirstStepToItsEnd()
    {
        var (game, animator) = Start(
            "#######",
            "#.....#",
            "#P....#",
            "#######");

        game.Press(Direction.Right);
        animator.Sync(game, 0);
        game.Press(Direction.Right);
        animator.Sync(game, 75);

        Assert.Equal(new PointF(2, 2), animator.PlayerPosition(75));
        Assert.Equal(new PointF(2.5f, 2), animator.PlayerPosition(150));
        Assert.Equal(new PointF(3, 2), animator.PlayerPosition(225));
    }

    [Fact]
    public void FallingIsOneContinuousGlideAtTickSpeed()
    {
        var (game, animator) = Start(
            "#####",
            "#.P.#",
            "#...#",
            "#...#",
            "#####");

        game.Tick();
        animator.Sync(game, 0);
        Assert.Equal(new PointF(2, 1.5f), animator.PlayerPosition(50));

        game.Tick();
        animator.Sync(game, 100);
        Assert.Equal(new PointF(2, 2), animator.PlayerPosition(100));
        Assert.Equal(new PointF(2, 2.5f), animator.PlayerPosition(150));
        Assert.Equal(new PointF(2, 3), animator.PlayerPosition(200));
    }

    [Fact]
    public void FallStartingMidStepBlendsFromWhereThePlayerIsDrawn()
    {
        var (game, animator) = Start(
            "######",
            "#P...#",
            "##...#",
            "#....#",
            "######");

        game.Press(Direction.Right); // Steps off the ledge.
        animator.Sync(game, 0);
        game.Tick();                 // Starts falling halfway through the step.
        animator.Sync(game, 75);

        Assert.Equal(new PointF(1.5f, 1), animator.PlayerPosition(75));
        Assert.Equal(new PointF(2, 2), animator.PlayerPosition(175));
    }

    [Fact]
    public void FallingAndSlidingThingsGlideBetweenCells()
    {
        var (game, animator) = Start(
            "#####",
            "#.I.#",
            "#...#",
            "#P..#",
            "#####");

        game.Tick();
        animator.Sync(game, 0);

        Assert.Equal(new PointF(2, 1.5f), animator.CellPosition(2, 2, 50));
        Assert.Equal(new PointF(2, 2), animator.CellPosition(2, 2, 100));
        Assert.Equal(new PointF(3, 3), animator.CellPosition(3, 3, 50)); // Things that didn't move stay put.
    }

    [Fact]
    public void KeyPressesDoNotCutObjectGlidesShort()
    {
        var (game, animator) = Start(
            "######",
            "#..I.#",
            "#....#",
            "#P...#",
            "######");

        game.Tick();
        animator.Sync(game, 0);
        game.Press(Direction.Left); // Just a turn.
        animator.Sync(game, 20);

        Assert.True(animator.IsAnimating(50));
        Assert.Equal(new PointF(3, 1.5f), animator.CellPosition(3, 2, 50));
    }

    [Fact]
    public void ResetJumpsStraightToTheStart()
    {
        var (game, animator) = Start(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        game.Press(Direction.Right);
        animator.Sync(game, 0);

        game.Reset();
        animator.Sync(game, 10);

        Assert.Equal(new PointF(1, 2), animator.PlayerPosition(10));
        Assert.False(animator.IsAnimating(10));
    }

    [Fact]
    public void GameReportsWhatMovedAndWhy()
    {
        GameState game = Game(
            "#######",
            "#.F...#",
            "#PC...#",
            "##III.#",
            "#######");
        Assert.Equal(ChangeKind.Reset, game.LastChange);

        game.Press(Direction.Right); // Push the crate.
        Assert.Equal(ChangeKind.Input, game.LastChange);
        Assert.Empty(game.LastMoves);

        game.Tick();
        Assert.Equal(ChangeKind.Tick, game.LastChange);
        Assert.Contains(new CellMove(2, 2, 3, 2), game.LastMoves); // Crate slides.
        Assert.Contains(new CellMove(2, 1, 2, 2), game.LastMoves); // Fire falls off it.
    }
}
