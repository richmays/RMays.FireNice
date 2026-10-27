using FireNice.Core;
using FireNice.Input;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class InputRepeaterTests
{
    private const int Interval = 333;

    private sealed class RecordingControls : IPlayerControls
    {
        public List<string> Calls { get; } = new();

        public void Press(Direction direction) => Calls.Add(direction == Direction.Left ? "L" : "R");

        public void UseAction() => Calls.Add("A");
    }

    [Fact]
    public void HeldDirectionFiresNowThenEveryInterval()
    {
        var controls = new RecordingControls();
        var repeater = new InputRepeater(controls, Interval);

        repeater.DirectionDown(Direction.Right, 0);
        repeater.Update(100);
        Assert.Equal(new[] { "R" }, controls.Calls);

        repeater.Update(333);
        repeater.Update(666);
        Assert.Equal(new[] { "R", "R", "R" }, controls.Calls);
    }

    [Fact]
    public void WindowsAutoRepeatIsIgnored()
    {
        var controls = new RecordingControls();
        var repeater = new InputRepeater(controls, Interval);

        repeater.DirectionDown(Direction.Right, 0);
        repeater.DirectionDown(Direction.Right, 30);
        repeater.DirectionDown(Direction.Right, 60);

        Assert.Equal(new[] { "R" }, controls.Calls);
    }

    [Fact]
    public void ReleasingTheKeyStopsRepeating()
    {
        var controls = new RecordingControls();
        var repeater = new InputRepeater(controls, Interval);

        repeater.DirectionDown(Direction.Right, 0);
        repeater.DirectionUp(Direction.Right, 100);
        repeater.Update(1000);

        Assert.Equal(new[] { "R" }, controls.Calls);
    }

    [Fact]
    public void LastPressedDirectionWins()
    {
        var controls = new RecordingControls();
        var repeater = new InputRepeater(controls, Interval);

        repeater.DirectionDown(Direction.Left, 0);
        repeater.DirectionDown(Direction.Right, 50);
        repeater.Update(383);
        Assert.Equal(new[] { "L", "R", "R" }, controls.Calls);

        repeater.DirectionUp(Direction.Right, 400); // Back to the left key, still held.
        Assert.Equal(new[] { "L", "R", "R", "L" }, controls.Calls);
    }

    [Fact]
    public void HeldActionRepeats()
    {
        var controls = new RecordingControls();
        var repeater = new InputRepeater(controls, Interval);

        repeater.ActionDown(0);
        repeater.Update(333);
        repeater.ActionUp();
        repeater.Update(1000);

        Assert.Equal(new[] { "A", "A" }, controls.Calls);
    }

    [Fact]
    public void HoldingTheOppositeDirectionTurnsPausesThenWalks()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#..P.#",
            "######");
        var repeater = new InputRepeater(game, Interval);

        repeater.DirectionDown(Direction.Left, 0);
        Assert.Equal(Direction.Left, game.Player.Facing);
        Assert.Equal(3, game.Player.X);

        repeater.Update(200);
        Assert.Equal(3, game.Player.X);

        repeater.Update(333);
        Assert.Equal(2, game.Player.X);

        repeater.Update(666);
        Assert.Equal(1, game.Player.X);
    }

    [Fact]
    public void HeldActionTogglesIceOnAndOff()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##...#",
            "#....#",
            "######");
        var repeater = new InputRepeater(game, Interval);

        repeater.ActionDown(0);
        Assert.True(game.Level[2, 3].IsIce);

        repeater.Update(333);
        Assert.Equal(CellType.Air, game.Level[2, 3].Type);

        repeater.Update(666);
        Assert.True(game.Level[2, 3].IsIce);
    }
}
