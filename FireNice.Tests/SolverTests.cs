using FireNice.Core;
using FireNice.IO;
using FireNice.UI;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class SolverTests
{
    private static SolveResult Solve(GameState game) =>
        Solver.Solve(game, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

    /// <summary>Plays the inputs the way playback does: everything settles before each press.</summary>
    private static void Replay(GameState game, IEnumerable<SolverInput> inputs)
    {
        Solver.Settle(game);
        foreach (SolverInput input in inputs)
        {
            Solver.Apply(game, input);
            Solver.Settle(game);
        }
    }

    [Fact]
    public void FindsTheShortestSolution()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.P..I.F.#",
            "##########");

        SolveResult result = Solve(game);

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.Equal(new[] { SolverInput.Right, SolverInput.Right, SolverInput.Right }, result.Inputs);
    }

    [Fact]
    public void SolverLeftStepsStraightAwayEvenWhenFacingRight()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#.P.#",
            "#####");

        Solver.Apply(game, SolverInput.Left);

        Assert.Equal(1, game.Player.X);
        Assert.Equal(Direction.Left, game.Player.Facing);
    }

    [Fact]
    public void ActionLeftAndActionRightWorkOnTheirOwnSideWhicheverWayThePlayerFaces()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#..P..#",
            "#..#..#",
            "#######");

        Solver.Apply(game, SolverInput.ActionLeft);
        Solver.Settle(game);
        Assert.True(game.Level[2, 3].IsIce);
        Assert.False(game.Level[4, 3].IsIce);
        Assert.Equal(Direction.Left, game.Player.Facing);

        Solver.Apply(game, SolverInput.ActionRight);
        Solver.Settle(game);
        Assert.True(game.Level[4, 3].IsIce);

        Solver.Apply(game, SolverInput.ActionLeft); // Already ice there: removes it.
        Solver.Settle(game);
        Assert.False(game.Level[2, 3].IsIce);
        Assert.True(game.Level[4, 3].IsIce);
    }

    [Fact]
    public void FindsASolutionThatGoesBothWaysWithoutTurning()
    {
        GameState game = Game(
            "#########",
            "#.......#",
            "#.F.I.P.#",
            "#########");

        SolveResult result = Solve(game);

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.Equal(new[] { SolverInput.Left, SolverInput.Left }, result.Inputs);
    }

    [Fact]
    public void PositionsThatDifferOnlyInFacingAreTheSame()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#.P.#",
            "#####");
        GameState turned = game.Clone();
        turned.Press(Direction.Left);
        int[] cells = { 6, 7, 8 };
        var packed = new ulong[1];
        var packedTurned = new ulong[1];

        game.Pack(cells, packed);
        turned.Pack(cells, packedTurned);

        Assert.Equal(packed, packedTurned);
    }

    [Fact]
    public void DoesNotChangeTheGameItSolves()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.P..I.F.#",
            "##########");
        string[] before = Rows(game);

        Solve(game);

        AssertRows(game, before);
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void SolvesFromTheMiddleOfALevel()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.P..I.F.#",
            "##########");
        game.Press(Direction.Left); // Turned the wrong way, which doesn't matter to the solver.

        SolveResult result = Solve(game);
        Replay(game, result.Inputs);

        Assert.Equal(3, result.Inputs.Count);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void ReportsImpossibleWhenNoFireCanBeReached()
    {
        GameState game = Game(
            "#######",
            "#P#.F.#",
            "#######");

        Assert.Equal(SolveOutcome.Impossible, Solve(game).Outcome);
    }

    [Fact]
    public void ReportsImpossibleAfterThePlayerHasDied()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PF.#",
            "#####");
        game.Press(Direction.Right);

        Assert.Equal(SolveOutcome.Impossible, Solve(game).Outcome);
    }

    [Fact]
    public void ReportsAlreadySolvedWhenNoFiresAreLeft()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI.F.#",
            "#######");
        game.Press(Direction.Right);
        Solver.Settle(game);

        Assert.Equal(SolveOutcome.AlreadySolved, Solve(game).Outcome);
    }

    [Fact]
    public void GivesUpWhenTimeRunsOut()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.P..I.F.#",
            "##########");

        SolveResult result = Solver.Solve(game, new CancellationToken(canceled: true));

        Assert.Equal(SolveOutcome.GaveUp, result.Outcome);
    }

    [Fact]
    public void SolvesStartingWhileThingsAreStillMoving()
    {
        GameState game = Game(
            "#######",
            "#.I...#",
            "#.....#",
            "#P..F.#",
            "#######");
        // The ice is mid-fall when Solve is pressed; it lands next to the player.

        SolveResult result = Solve(game);
        Replay(game, result.Inputs);

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Theory]
    [InlineData("level01.txt", 3)]
    [InlineData("level02.txt", 2)]
    [InlineData("level03.txt", 10)]
    [InlineData("level04.txt", 6)]
    [InlineData("level05.txt", 11)]
    [InlineData("level06.txt", 4)]
    [InlineData("level07.txt", 5)]
    public void SolvesEachBuiltInLevelInNoMorePressesThanTheKnownSolution(string file, int knownPresses)
    {
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), file)));

        SolveResult result = Solve(game);
        Replay(game, result.Inputs);

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.True(result.Inputs.Count <= knownPresses, $"{result.Inputs.Count} presses");
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Theory]
    [InlineData("level01.txt")]
    [InlineData("level03.txt")]
    [InlineData("level05.txt")]
    [InlineData("level07.txt")]
    [InlineData("level_pipes.txt")]
    public void FallsBackToAnySolutionWhenTheShortestSearchRunsOutOfTime(string file)
    {
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), file)));

        // No time at all for the shortest search, so the fallback has to find it.
        SolveResult result = Solver.Solve(
            game, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token, shortestTimeLimit: TimeSpan.Zero);
        Replay(game, result.Inputs);

        Assert.Equal(SolveOutcome.SolvedNotShortest, result.Outcome);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void PrefersTheShortestSolutionWhenThereIsTime()
    {
        var game = new GameState(LevelReader.Load(Path.Combine(LevelsFolder(), "level05.txt")));

        SolveResult result = Solver.Solve(
            game, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token, shortestTimeLimit: TimeSpan.FromSeconds(15));

        Assert.Equal(SolveOutcome.Solved, result.Outcome);
        Assert.Equal(11, result.Inputs.Count);
    }

    [Fact]
    public void FallbackStillReportsImpossibleLevels()
    {
        GameState game = Game(
            "#######",
            "#P#.F.#",
            "#######");

        SolveResult result = Solver.Solve(
            game, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token, shortestTimeLimit: TimeSpan.Zero);

        Assert.Equal(SolveOutcome.Impossible, result.Outcome);
    }

    [Fact]
    public void PackedPositionsUnpackToTheSameGame()
    {
        var random = new Random(1234);
        foreach (string file in Directory.GetFiles(LevelsFolder(), "*.txt"))
        {
            var game = new GameState(LevelReader.Load(file));
            Solver.Settle(game);
            int[] cells = Enumerable.Range(0, game.Level.Width * game.Level.Height)
                .Where(i => game.Level[i % game.Level.Width, i / game.Level.Width] is var c
                    && c.Type != CellType.Block && !c.IsPipe)
                .ToArray();
            var packed = new ulong[(GameState.PackedBits(cells.Length) + 63) / 64];
            GameState copy = game.Clone();

            for (int press = 0; press < 300 && game.Status == GameStatus.Playing; press++)
            {
                Solver.Apply(game, (SolverInput)random.Next(6));
                Solver.Settle(game);
                if (game.Status != GameStatus.Playing)
                {
                    break;
                }

                game.Pack(cells, packed);
                copy.Unpack(cells, packed);
                copy.Player.Facing = game.Player.Facing; // Not packed: the solver's inputs don't depend on it.
                Assert.Equal(game.StateKey(), copy.StateKey());
                Assert.Equal(game.CanAct, copy.CanAct);
            }
        }
    }

    [Fact]
    public void PlaybackReplaysTheSolutionOnTheRealGame()
    {
        GameState game = Game(
            "##########",
            "#........#",
            "#.P..I.F.#",
            "##########");
        SolveResult result = Solve(game);
        var playback = new Playback(game, result.Inputs, tickMs: 50, keyGapMs: 166, now: 0);

        long now = 0;
        while (!playback.IsFinished && now < 60_000)
        {
            now += 15;
            playback.Update(now);
        }

        Assert.True(playback.IsFinished);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void PlaybackWaitsForThingsToSettleBeforeEachPress()
    {
        GameState game = Game(
            "########",
            "#P.....#",
            "##.....#",
            "#......#",
            "#...I.F#",
            "########");
        var playback = new Playback(game, new[] { SolverInput.Right, SolverInput.Right }, tickMs: 50, keyGapMs: 10, now: 0);

        playback.Update(60);  // Settles, so the first press happens: steps off the ledge.
        playback.Update(110); // Falling. The second press must wait.
        Assert.Equal(2, game.Player.X);

        long now = 110;
        while (!playback.IsFinished && now < 10_000)
        {
            now += 15;
            playback.Update(now);
        }

        Assert.Equal(3, game.Player.X); // Second press happened after landing.
    }
}
