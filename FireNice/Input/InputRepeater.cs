using FireNice.Core;

namespace FireNice.Input;

/// <summary>
/// Turns held keys into repeated game input, at a fixed interval that doesn't depend on
/// Windows key auto-repeat. Times are in milliseconds from any steady clock.
/// </summary>
/// <remarks>
/// Holding the direction the player faces steps now and then every interval. Holding the
/// opposite direction turns now, and the next repeat (one interval later) starts walking, which
/// gives the "turn, pause, walk" behavior. If both directions are held, the last pressed wins.
/// </remarks>
public sealed class InputRepeater
{
    private readonly IPlayerControls controls;
    private readonly List<Direction> heldDirections = new();
    private long nextDirectionAt;
    private bool actionHeld;
    private long nextActionAt;

    public InputRepeater(IPlayerControls controls, int intervalMs)
    {
        this.controls = controls;
        IntervalMs = intervalMs;
    }

    public int IntervalMs { get; }

    public void DirectionDown(Direction direction, long now)
    {
        if (heldDirections.Contains(direction))
        {
            return; // Windows auto-repeat; the timer handles repeating.
        }

        heldDirections.Add(direction);
        controls.Press(direction);
        nextDirectionAt = now + IntervalMs;
    }

    public void DirectionUp(Direction direction, long now)
    {
        bool wasActive = heldDirections.Count > 0 && heldDirections[^1] == direction;
        heldDirections.Remove(direction);
        if (wasActive && heldDirections.Count > 0)
        {
            // Hand control back to the other key that's still held.
            controls.Press(heldDirections[^1]);
            nextDirectionAt = now + IntervalMs;
        }
    }

    public void ActionDown(long now)
    {
        if (actionHeld)
        {
            return;
        }

        actionHeld = true;
        controls.UseAction();
        nextActionAt = now + IntervalMs;
    }

    public void ActionUp() => actionHeld = false;

    /// <summary>Fires any repeats that are due. Call this often (every frame).</summary>
    public void Update(long now)
    {
        if (heldDirections.Count > 0 && now >= nextDirectionAt)
        {
            controls.Press(heldDirections[^1]);
            nextDirectionAt = now + IntervalMs;
        }

        if (actionHeld && now >= nextActionAt)
        {
            controls.UseAction();
            nextActionAt = now + IntervalMs;
        }
    }

    public void ReleaseAll()
    {
        heldDirections.Clear();
        actionHeld = false;
    }
}
