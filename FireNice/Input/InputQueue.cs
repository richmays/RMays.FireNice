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
    private readonly Queue<SolverInput> waiting = new();

    public InputQueue(GameState game)
    {
        this.game = game;
    }

    /// <summary>How many presses are waiting.</summary>
    public int Count => waiting.Count;

    /// <summary>Nothing is waiting and the game would take a key right now.</summary>
    public bool IsIdle => waiting.Count == 0 && game.CanAct;

    public void Press(Direction direction) =>
        Add(direction == Direction.Left ? SolverInput.Left : SolverInput.Right);

    public void UseAction() => Add(SolverInput.Action);

    public void PressUp() => Add(SolverInput.Up);

    public void PressDown() => Add(SolverInput.Down);

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
            Solver.Apply(game, waiting.Dequeue());
        }
    }

    /// <summary>Drops everything waiting (for example on reset or rewind).</summary>
    public void Clear() => waiting.Clear();

    private void Add(SolverInput input)
    {
        waiting.Enqueue(input);
        Pump();
    }
}
