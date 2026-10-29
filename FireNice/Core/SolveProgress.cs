namespace FireNice.Core;

/// <summary>Which of the two searches <see cref="Solver.Solve"/> is running.</summary>
public enum SolveStage
{
    /// <summary>The breadth-first search for a shortest solution.</summary>
    Shortest,

    /// <summary>The fallback search for any solution, after the shortest search ran out of time.</summary>
    AnySolution,
}

/// <summary>
/// How far a running <see cref="Solver.Solve"/> has got. The solver updates it as it goes; it's
/// safe to read from another thread (for example a window showing progress) at any time.
/// </summary>
public sealed class SolveProgress
{
    private int stage;
    private int depth;
    private long positionsChecked;

    public SolveStage Stage => (SolveStage)Volatile.Read(ref stage);

    /// <summary>
    /// How many key presses deep the search is. In the shortest search, solutions of this many
    /// presses are being looked for, and every shorter one has been ruled out. In the fallback
    /// search, the most presses of any position it has reached.
    /// </summary>
    public int Depth => Volatile.Read(ref depth);

    /// <summary>
    /// Positions whose key presses have all been tried, in both searches together. Each position is
    /// counted once per search, however many ways it was reached.
    /// </summary>
    public long PositionsChecked => Interlocked.Read(ref positionsChecked);

    internal void StartStage(SolveStage newStage)
    {
        Volatile.Write(ref depth, 0);
        Volatile.Write(ref stage, (int)newStage);
    }

    internal void SetDepth(int presses) => Volatile.Write(ref depth, presses);

    internal void RaiseDepth(int presses)
    {
        if (presses > Volatile.Read(ref depth))
        {
            Volatile.Write(ref depth, presses); // Only the fallback calls this, from one thread.
        }
    }

    internal void AddChecked(long count) => Interlocked.Add(ref positionsChecked, count);
}
