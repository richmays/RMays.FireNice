using FireNice.Core;

namespace FireNice.UI;

/// <summary>
/// Replays a solver's key presses on the real game. While it runs, it drives the game clock
/// itself (so it can run faster than normal). Like the solver, it waits for everything to settle
/// before each press, so the replay plays out exactly as the solver saw it.
/// </summary>
public sealed class Playback
{
    private readonly GameState game;
    private readonly IReadOnlyList<SolverInput> inputs;
    private readonly int tickMs;
    private readonly int keyGapMs;
    private int next;
    private long nextTickAt;
    private long nextKeyAt;
    private bool settled;

    public Playback(GameState game, IReadOnlyList<SolverInput> inputs, int tickMs, int keyGapMs, long now)
    {
        this.game = game;
        this.inputs = inputs;
        this.tickMs = Math.Max(1, tickMs);
        this.keyGapMs = keyGapMs;
        nextTickAt = now + this.tickMs;
        nextKeyAt = now + keyGapMs;
    }

    /// <summary>All presses have been made and everything has come to rest.</summary>
    public bool IsFinished => next >= inputs.Count && settled;

    public void Update(long now)
    {
        int ticks = 0;
        while (now >= nextTickAt && ticks < 5)
        {
            settled = !game.Tick();
            nextTickAt += tickMs;
            ticks++;
        }

        if (now >= nextTickAt)
        {
            nextTickAt = now + tickMs;
        }

        if (settled && next < inputs.Count && now >= nextKeyAt)
        {
            Solver.Apply(game, inputs[next++]);
            settled = false;
            nextKeyAt = now + keyGapMs;
        }
    }
}
