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
/// <item>A rewind plays the player's path backwards at double speed: each cell takes half as long
/// as it did going forwards. Things that moved glide straight back, half a tick per cell.</item>
/// <item>A reset or a new level jumps straight to the new positions.</item>
/// </list>
/// </remarks>
public sealed class Animator
{
    private GameState? game;

    // The player glides through these points in order, spending the matching time on each leg.
    private PointF[] playerPath = { PointF.Empty };
    private int[] playerLegMs = Array.Empty<int>();
    private long playerStart;

    // Keyed by the cell a thing is in now: where it glides in from, and how long that takes.
    private Dictionary<Point, (Point From, int Ms)> objectFrom = new();
    private long objectStart;
    private int objectMs;

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
            playerPath = new[] { playerCell };
            playerLegMs = Array.Empty<int>();
            objectFrom = new Dictionary<Point, (Point, int)>();
            return;
        }

        if (current.LastChange == ChangeKind.Rewind)
        {
            SyncRewind(current, now);
            return;
        }

        bool fromTick = current.LastChange == ChangeKind.Tick;
        if (fromTick && current.LastMoves.Count > 0)
        {
            objectFrom = current.LastMoves.ToDictionary(m => new Point(m.ToX, m.ToY), m => (new Point(m.FromX, m.FromY), TickMs));
            objectStart = now;
            objectMs = TickMs;
        }

        if (playerCell != playerPath[^1])
        {
            // Ticks blend from where the player is drawn now; key presses skip the last step ahead.
            PointF from = fromTick ? PlayerPosition(now) : playerPath[^1];
            playerPath = new[] { from, playerCell };
            playerLegMs = new[] { fromTick ? TickMs : WalkMs };
            playerStart = now;
        }
    }

    /// <summary>Where to draw the player, in cell units.</summary>
    public PointF PlayerPosition(long now)
    {
        long elapsed = now - playerStart;
        for (int i = 0; i < playerLegMs.Length; i++)
        {
            int ms = playerLegMs[i];
            if (elapsed < ms)
            {
                return Lerp(playerPath[i], playerPath[i + 1], Progress(elapsed, ms));
            }

            elapsed -= ms;
        }

        return playerPath[^1];
    }

    /// <summary>
    /// Where to draw whatever is in cell (x, y), in cell units. It's the cell itself unless the
    /// thing there is still gliding in from another cell.
    /// </summary>
    public PointF CellPosition(int x, int y, long now)
    {
        if (!objectFrom.TryGetValue(new Point(x, y), out var glide))
        {
            return new PointF(x, y);
        }

        return Lerp(glide.From, new PointF(x, y), Progress(now - objectStart, glide.Ms));
    }

    /// <summary>True while anything is between cells, so the view needs redrawing.</summary>
    public bool IsAnimating(long now) =>
        now - playerStart < playerLegMs.Sum()
        || (objectFrom.Count > 0 && now - objectStart < objectMs);

    private void SyncRewind(GameState current, long now)
    {
        // Start from wherever the player is drawn, then retrace the cells they went through.
        // Each leg takes half as long as the move it undoes.
        IReadOnlyList<RewindStep> path = current.LastRewindPath;
        var points = new List<PointF> { PlayerPosition(now) };
        var legs = new List<int>();
        for (int i = 1; i < path.Count; i++)
        {
            points.Add(new PointF(path[i].X, path[i].Y));
            legs.Add((path[i - 1].CameBy == ChangeKind.Input ? WalkMs : TickMs) / 2);
        }

        if (points.Count == 1)
        {
            points.Add(new PointF(current.Player.X, current.Player.Y)); // Only turned; jump if drawn elsewhere.
            legs.Add(0);
        }

        playerPath = points.ToArray();
        playerLegMs = legs.ToArray();
        playerStart = now;

        objectFrom = new Dictionary<Point, (Point, int)>();
        objectMs = 0;
        foreach (CellMove m in current.LastMoves)
        {
            int ms = (Math.Abs(m.ToX - m.FromX) + Math.Abs(m.ToY - m.FromY)) * TickMs / 2;
            objectFrom[new Point(m.ToX, m.ToY)] = (new Point(m.FromX, m.FromY), ms);
            objectMs = Math.Max(objectMs, ms);
        }

        objectStart = now;
    }

    private static PointF Lerp(PointF from, PointF to, float t) =>
        new(from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t));

    private static float Progress(long elapsed, int duration) =>
        duration <= 0 ? 1f : Math.Clamp(elapsed / (float)duration, 0f, 1f);
}
