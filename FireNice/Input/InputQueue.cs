using FireNice.Core;

namespace FireNice.Input;

/// <summary>
/// Holds key presses until the game can take them. The game ignores input while anything is moving
/// (<see cref="GameState.CanAct"/>), so a key pressed then waits here and is applied once everything
/// has stopped, in the order the keys were pressed, one at a time: if a press sets something moving,
/// the next waits for that to stop too.
/// </summary>
public sealed class InputQueue : IPlayerControls
{
    private readonly GameState game;
    private readonly Queue<Key> waiting = new();

    public InputQueue(GameState game)
    {
        this.game = game;
    }

    /// <summary>How many presses are waiting.</summary>
    public int Count => waiting.Count;

    /// <summary>Nothing is waiting and the game would take a key right now.</summary>
    public bool IsIdle => waiting.Count == 0 && game.CanAct;

    public void Press(Direction direction) =>
        Add(direction == Direction.Left ? Key.Left : Key.Right);

    public void UseAction() => Add(Key.Action);

    public void PressUp() => Add(Key.Up);

    public void PressDown() => Add(Key.Down);

    /// <summary>
    /// Applies waiting presses while the game can take them. Call after every tick. Once the level
    /// is won or lost, anything still waiting is dropped.
    /// </summary>
    public void Pump()
    {
        if (game.Status != GameStatus.Playing)
        {
            waiting.Clear();
            return;
        }

        while (waiting.Count > 0 && game.CanAct)
        {
            Apply(waiting.Dequeue());
        }
    }

    /// <summary>Drops everything waiting (for example on reset or rewind).</summary>
    public void Clear() => waiting.Clear();

    /// <summary>
    /// The keyboard keys. These aren't <see cref="SolverInput"/>s: a keyboard Left or Right only turns
    /// the player if they're facing the other way, and the one action key works on whichever side
    /// they face.
    /// </summary>
    private enum Key
    {
        Left,
        Right,
        Up,
        Down,
        Action,
    }

    private void Apply(Key key)
    {
        switch (key)
        {
            case Key.Left:
                game.Press(Direction.Left);
                break;
            case Key.Right:
                game.Press(Direction.Right);
                break;
            case Key.Up:
                game.PressUp();
                break;
            case Key.Down:
                game.PressDown();
                break;
            case Key.Action:
                game.UseAction();
                break;
        }
    }

    private void Add(Key input)
    {
        waiting.Enqueue(input);
        Pump();
    }
}
