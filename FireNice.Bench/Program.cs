// Solver benchmark: solves every level file and reports time and positions per second.
// Usage: dotnet run -c Release --project FireNice.Bench -- [seconds per level, default 60] [file filter]
//        [seconds for the shortest search, default half; 0 tries only the fallback]
using System.Diagnostics;
using FireNice.Core;
using FireNice.IO;

int seconds = args.Length > 0 ? int.Parse(args[0]) : 60;
string filter = args.Length > 1 ? args[1] : "*.txt";
double shortestSeconds = args.Length > 2 ? double.Parse(args[2]) : seconds / 2.0;
string levels = Path.Combine(AppContext.BaseDirectory, "Levels");

Console.WriteLine($"{"Level",-18} {"Outcome",-17} {"Presses",7} {"Positions",11} {"Seconds",8} {"Pos/s",10}");
foreach (string file in Directory.GetFiles(levels, filter).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
{
    var game = new GameState(LevelReader.Load(file));
    using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
    var watch = Stopwatch.StartNew();
    // Like the game, the shortest search gets half the time by default and the fallback the rest.
    SolveResult result = Solver.Solve(game, cancel.Token, TimeSpan.FromSeconds(shortestSeconds));
    double elapsed = watch.Elapsed.TotalSeconds;
    Console.WriteLine(
        $"{Path.GetFileName(file),-18} {result.Outcome,-17} {result.Inputs.Count,7} {result.PositionsChecked,11:N0} " +
        $"{elapsed,8:F2} {result.PositionsChecked / Math.Max(elapsed, 1e-6),10:N0}");
}

Console.WriteLine($"Peak memory: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N0} MB");
