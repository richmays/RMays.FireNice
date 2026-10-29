using FireNice.Core;
using FireNice.Input;
using FireNice.IO;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class InputQueueTests
{
    /// <summary>Runs the clock the way the window does: a tick, then any waiting presses.</summary>
    private static void RunClock(GameState game, InputQueue queue, int maxTicks = 500)
    {
        for (int i = 0; i < maxTicks; i++)
        {
            bool moved = game.Tick();
            queue.Pump();
            if (!moved && queue.Count == 0 && game.CanAct)
            {
                return;
            }
        }
    }

    [Fact]
    public void AStepPressedWhileIceSlidesHappensOnceItHasPutOutTheFire()
    {
        // Round 1-1: walk left, push the ice into the fire, and press Left again straight away.
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), "level_1-1.txt")));
        var queue = new InputQueue(game);
        RunClock(game, queue);
        queue.Press(Direction.Left); // Turn.
        queue.Press(Direction.Left); // Step to (6, 2), next to the ice.
        Assert.Equal((6, 2), (game.Player.X, game.Player.Y));

        queue.Press(Direction.Left); // Push: the ice slides off toward the fire.
        queue.Press(Direction.Left); // Pressed at once; it has to wait.
        Assert.Equal(1, queue.Count);
        Assert.Equal((6, 2), (game.Player.X, game.Player.Y));

        RunClock(game, queue);

        Assert.Equal('.', Rows(game)[2][3]); // The fire is out.
        Assert.Equal((5, 2), (game.Player.X, game.Player.Y)); // And then the step happened.
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void WaitingPressesAreAppliedInOrderEachAfterThingsStop()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.PI.....#",
            "##########");
        var queue = new InputQueue(game);

        queue.Press(Direction.Right); // Push: the ice slides away.
        queue.Press(Direction.Right); // Step.
        queue.Press(Direction.Left);  // Turn.
        queue.UseAction();            // Make ice in front, to the left and down: the floor, so nothing.
        queue.Press(Direction.Left);  // Step back.
        Assert.Equal(4, queue.Count);

        RunClock(game, queue);

        Assert.Equal((2, 2), (game.Player.X, game.Player.Y));
        Assert.Equal(Direction.Left, game.Player.Facing);
        Assert.Equal('I', Rows(game)[2][8]); // The ice slid all the way.
    }

    [Fact]
    public void PressesWorkAtOnceWhenNothingIsMoving()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#P....#",
            "#######");
        var queue = new InputQueue(game);

        queue.Press(Direction.Right);
        queue.Press(Direction.Right);

        Assert.Equal(3, game.Player.X);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void WaitingPressesAreDroppedWhenTheLevelEnds()
    {
        GameState game = Game(
            "#########",
            "#.......#",
            "#.PI.F..#",
            "#########");
        var queue = new InputQueue(game);

        queue.Press(Direction.Right); // Push the ice into the fire: that wins.
        queue.Press(Direction.Right); // Waits, then the level is won.
        RunClock(game, queue);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(0, queue.Count);
        Assert.Equal(2, game.Player.X);
    }

    [Fact]
    public void ClearDropsWaitingPresses()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.PI.....#",
            "##########");
        var queue = new InputQueue(game);
        queue.Press(Direction.Right);
        queue.Press(Direction.Right);

        queue.Clear();
        RunClock(game, queue);

        Assert.Equal(2, game.Player.X);
    }

    [Fact]
    public void HeldKeyRepeatsWaitInsteadOfPilingUp()
    {
        var ready = false;
        var calls = new List<Direction>();
        var controls = new RecordingControls(calls);
        var repeater = new InputRepeater(controls, 333, () => ready);

        repeater.DirectionDown(Direction.Right, 0); // The first press always goes through.
        repeater.Update(333);
        repeater.Update(666);
        repeater.Update(999); // Three repeats due while not ready: none fire.
        Assert.Single(calls);

        ready = true;
        repeater.Update(1000); // One repeat, as soon as it's ready...
        repeater.Update(1100); // ...and the next one a full interval later.
        Assert.Equal(2, calls.Count);
        repeater.Update(1333);
        Assert.Equal(3, calls.Count);
    }

    private sealed class RecordingControls : IPlayerControls
    {
        private readonly List<Direction> calls;

        public RecordingControls(List<Direction> calls) => this.calls = calls;

        public void Press(Direction direction) => calls.Add(direction);

        public void UseAction()
        {
        }
    }
}
