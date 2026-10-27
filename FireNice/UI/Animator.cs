using System.Drawing;
using FireNice.Core;

namespace FireNice.UI;

/// <summary>
/// Works out where things should be drawn between cells, so movement looks smooth.
/// The game itself moves in whole cells; this only affects drawing. Positions are in cell
/// units (x, y). Times are milliseconds from any steady clock.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Player steps and climbs (from key presses) take <see cref="WalkMs"/>. A new key press
/// skips the current step to its end, so quick presses never fall behind.</item>
/// <item>Falling, sliding and pipe exits (from ticks) take exactly one tick, and start from wherever
/// the thing is currently drawn, so a fall reads as one continuous glide.</item>
/// <item>A reset or a new level jumps straight to the new positions.</item>
/// </list>
/// </remarks>
public sealed class Animator
{
    private GameState? game;

    private PointF playerFrom;
    private PointF playerTo;
    private long playerStart;
    private int playerDuration;

    private Dictionary<Point, Point> objectFrom = new();
    private long objectStart;

    public Animator(int walkMs, int tickMs)
    {
        WalkMs = walkMs;
        TickMs = tickMs;
    }

    /// <summary>Step length; can be changed on the fly (solution playback runs at double speed).</summary>
    public int WalkMs { get; set; }

    /// <summary>Tick length; can be changed on the fly (solution playback runs at double speed).</summary>
    public int TickMs { get; set; }

    /// <summary>Call after every change to the game (and when switching to a new game).</summary>
    public void Sync(GameState current, long now)
    {
        var playerCell = new PointF(current.Player.X, current.Player.Y);

        if (!ReferenceEquals(current, game) || current.LastChange == ChangeKind.Reset)
        {
            game = current;
            playerFrom = playerTo = playerCell;
            playerDuration = 0;
            objectFrom = new Dictionary<Point, Point>();
            return;
        }

        bool fromTick = current.LastChange == ChangeKind.Tick;
        if (fromTick && current.LastMoves.Count > 0)
        {
            objectFrom = current.LastMoves.ToDictionary(m => new Point(m.ToX, m.ToY), m => new Point(m.FromX, m.FromY));
            objectStart = now;
        }

        if (playerCell != playerTo)
        {
            // Ticks blend from where the player is drawn now; key presses skip the last step ahead.
            playerFrom = fromTick ? PlayerPosition(now) : playerTo;
            playerTo = playerCell;
            playerStart = now;
            playerDuration = fromTick ? TickMs : WalkMs;
        }
    }

    /// <summary>Where to draw the player, in cell units.</summary>
    public PointF PlayerPosition(long now)
    {
        float t = Progress(now, playerStart, playerDuration);
        return new PointF(
            playerFrom.X + ((playerTo.X - playerFrom.X) * t),
            playerFrom.Y + ((playerTo.Y - playerFrom.Y) * t));
    }

    /// <summary>
    /// Where to draw whatever is in cell (x, y), in cell units. It's the cell itself unless the
    /// thing there is still gliding in from another cell.
    /// </summary>
    public PointF CellPosition(int x, int y, long now)
    {
        if (!objectFrom.TryGetValue(new Point(x, y), out Point from))
        {
            return new PointF(x, y);
        }

        float t = Progress(now, objectStart, TickMs);
        return new PointF(from.X + ((x - from.X) * t), from.Y + ((y - from.Y) * t));
    }

    /// <summary>True while anything is between cells, so the view needs redrawing.</summary>
    public bool IsAnimating(long now) =>
        Progress(now, playerStart, playerDuration) < 1f
        || (objectFrom.Count > 0 && Progress(now, objectStart, TickMs) < 1f);

    private static float Progress(long now, long start, int duration) =>
        duration <= 0 ? 1f : Math.Clamp((now - start) / (float)duration, 0f, 1f);
}
