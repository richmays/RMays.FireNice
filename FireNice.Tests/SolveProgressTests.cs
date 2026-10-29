using System.Diagnostics;
using FireNice.Core;
using FireNice.IO;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class SolveProgressTests
{
    private static GameState Load(string file) => new(LevelReader.Load(Path.Combine(LevelsFolder(), file)));

    [Fact]
    public void ShortestSearchReportsTheDepthItFoundTheSolutionAt()
    {
        var progress = new SolveProgress();

        SolveResult result = Solver.Solve(
            Load("level05.txt"), new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token, progress: progress);

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.Equal(SolveStage.Shortest, progress.Stage);
        Assert.Equal(result.Inputs.Count, progress.Depth);
        Assert.InRange(progress.PositionsChecked, 1, result.PositionsChecked);
    }

    [Fact]
    public void FallbackSearchReportsItsStageAndHowDeepItGot()
    {
        var progress = new SolveProgress();

        SolveResult result = Solver.Solve(
            Load("level05.txt"),
            new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token,
            shortestTimeLimit: TimeSpan.Zero,
            progress: progress);

        Assert.Equal(SolveOutcome.SolvedNotShortest, result.Outcome);
        Assert.Equal(SolveStage.AnySolution, progress.Stage);
        Assert.InRange(progress.Depth, 1, result.Inputs.Count);
        Assert.True(progress.PositionsChecked > 0);
    }

    [Fact]
    public void ProgressKeepsGrowingWhileTheSearchRuns()
    {
        var progress = new SolveProgress();
        using var cancel = new CancellationTokenSource();
        Task<SolveResult> solving = Task.Run(() => Solver.Solve(Load("level_10-6.txt"), cancel.Token, progress: progress));

        Thread.Sleep(1000);
        long early = progress.PositionsChecked;
        int earlyDepth = progress.Depth;
        Thread.Sleep(1000);
        cancel.Cancel();
        solving.Wait();

        Assert.True(early > 0);
        Assert.True(progress.PositionsChecked > early);
        Assert.True(progress.Depth >= earlyDepth && earlyDepth > 0);
    }

    [Fact]
    public void CancellingStopsALongSearchQuickly()
    {
        using var cancel = new CancellationTokenSource();
        Task<SolveResult> solving = Task.Run(() => Solver.Solve(Load("level_10-6.txt"), cancel.Token));
        Thread.Sleep(3000); // Deep enough that each layer takes a while.

        var watch = Stopwatch.StartNew();
        cancel.Cancel();
        SolveResult result = solving.Result;

        Assert.Equal(SolveOutcome.GaveUp, result.Outcome);
        Assert.True(watch.ElapsedMilliseconds < 1000, $"Took {watch.ElapsedMilliseconds} ms to stop.");
    }
}
