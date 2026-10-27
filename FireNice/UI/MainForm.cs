using System.Diagnostics;
using FireNice.Core;
using FireNice.Input;
using FireNice.IO;

namespace FireNice.UI;

public sealed class MainForm : Form
{
    private readonly Settings settings;
    private readonly Dictionary<Keys, GameAction> keyMap;
    private readonly GamePanel panel = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel levelLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel firesLabel = new();
    private readonly ToolStripMenuItem nextLevelItem;
    private readonly ToolStripMenuItem solveItem;
    private readonly System.Windows.Forms.Timer clock = new() { Interval = 15 };
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly List<string> levelPaths;
    private readonly Animator animator;

    private GameState? game;
    private InputRepeater? repeater;
    private int levelIndex = -1;
    private long nextTickAt;
    private bool wasAnimating;

    // Holding Rewind keeps undoing, twice as fast as walking repeats.
    private bool rewindHeld;
    private long nextRewindAt;

    // Solve runs in the background; game keys are ignored until it finishes.
    private CancellationTokenSource? solveCancel;
    private int solveRun;

    // Set while a found solution is being replayed.
    private Playback? playback;

    public MainForm()
    {
        Text = "FireNice";
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(400, 300);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        settings = Settings.LoadOrCreate(Path.Combine(AppContext.BaseDirectory, "keybindings.json"));
        keyMap = settings.BuildKeyMap();
        animator = new Animator(settings.WalkAnimationMs, settings.TickMs);
        panel.Animator = animator;
        panel.Now = () => Now;

        string levelsFolder = Path.Combine(AppContext.BaseDirectory, "Levels");
        levelPaths = Directory.Exists(levelsFolder)
            ? Directory.GetFiles(levelsFolder, "*.txt").OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()
            : new List<string>();

        var resetItem = new ToolStripMenuItem("&Reset Level", null, (_, _) => ResetLevel()) { ShortcutKeyDisplayString = "R" };
        var rewindItem = new ToolStripMenuItem("Re&wind One Step", null, (_, _) => RewindOneStep()) { ShortcutKeyDisplayString = "Z" };
        var loadItem = new ToolStripMenuItem("&Load Level...", null, (_, _) => LoadLevelFromDialog());
        nextLevelItem = new ToolStripMenuItem("&Next Level", null, (_, _) => NextLevel()) { ShortcutKeyDisplayString = "Enter" };
        solveItem = new ToolStripMenuItem("&Solve", null, (_, _) => Solve());
        var exitItem = new ToolStripMenuItem("E&xit", null, (_, _) => Close());
        var gameMenu = new ToolStripMenuItem("&Game");
        gameMenu.DropDownItems.AddRange(new ToolStripItem[] { resetItem, rewindItem, solveItem, loadItem, nextLevelItem, new ToolStripSeparator(), exitItem });
        var menu = new MenuStrip();
        menu.Items.Add(gameMenu);

        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        toolbar.Items.Add(new ToolStripButton("Reset", null, (_, _) => ResetLevel()) { ToolTipText = "Put the level back to how it started (R)" });
        toolbar.Items.Add(new ToolStripButton("Rewind", null, (_, _) => RewindOneStep()) { ToolTipText = "Undo your last move; hold Z to keep going back (Z)" });

        var status = new StatusStrip();
        status.Items.AddRange(new ToolStripItem[] { levelLabel, firesLabel });

        Controls.Add(panel);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(status);
        MainMenuStrip = menu;

        clock.Tick += (_, _) => OnClock();
        Deactivate += (_, _) =>
        {
            repeater?.ReleaseAll();
            rewindHeld = false;
        };
        Shown += (_, _) => StartFirstLevel();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Modifiers) == Keys.None && keyMap.TryGetValue(keyData & Keys.KeyCode, out GameAction action))
        {
            OnActionDown(action);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (keyMap.TryGetValue(e.KeyCode, out GameAction action))
        {
            OnActionUp(action);
            e.Handled = true;
        }

        base.OnKeyUp(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        clock.Stop();
        base.OnFormClosed(e);
    }

    private long Now => stopwatch.ElapsedMilliseconds;

    private void OnActionDown(GameAction action)
    {
        if (action == GameAction.Rewind)
        {
            if (!rewindHeld)
            {
                // Windows auto-repeat is ignored; the clock repeats while the key is held.
                rewindHeld = true;
                RewindOneStep();
                nextRewindAt = Now + RewindRepeatMs;
            }

            return;
        }

        if (playback is not null)
        {
            // Any game key stops the replay where it is; Reset also resets.
            StopPlayback();
            if (action == GameAction.Reset)
            {
                ResetLevel();
            }

            return;
        }

        if (solveCancel is not null && action != GameAction.Reset)
        {
            return; // Still solving.
        }

        switch (action)
        {
            case GameAction.MoveLeft:
                repeater?.DirectionDown(Direction.Left, Now);
                break;
            case GameAction.MoveRight:
                repeater?.DirectionDown(Direction.Right, Now);
                break;
            case GameAction.MoveUp:
                game?.PressUp();
                break;
            case GameAction.MoveDown:
                game?.PressDown();
                break;
            case GameAction.Action:
                repeater?.ActionDown(Now);
                break;
            case GameAction.Reset:
                ResetLevel();
                break;
            case GameAction.NextLevel:
                if (game?.Status == GameStatus.Won)
                {
                    NextLevel();
                }

                break;
        }
    }

    private void OnActionUp(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveLeft:
                repeater?.DirectionUp(Direction.Left, Now);
                break;
            case GameAction.MoveRight:
                repeater?.DirectionUp(Direction.Right, Now);
                break;
            case GameAction.Action:
                repeater?.ActionUp();
                break;
            case GameAction.Rewind:
                rewindHeld = false;
                break;
        }
    }

    private int RewindRepeatMs => settings.RepeatIntervalMs / 2;

    private void OnClock()
    {
        if (game is null || repeater is null)
        {
            return;
        }

        long now = Now;
        if (playback is not null)
        {
            playback.Update(now);
            if (playback.IsFinished)
            {
                StopPlayback();
            }
        }
        else
        {
            if (rewindHeld && now >= nextRewindAt)
            {
                RewindOneStep();
                nextRewindAt = now + RewindRepeatMs;
            }

            repeater.Update(now);

            // Catch up on missed ticks, but not forever if the window was stalled.
            int ticks = 0;
            while (now >= nextTickAt && ticks < 5)
            {
                game.Tick();
                nextTickAt += settings.TickMs;
                ticks++;
            }

            if (now >= nextTickAt)
            {
                nextTickAt = now + settings.TickMs;
            }
        }

        // Keep redrawing while anything is between cells, plus one more frame to land exactly.
        bool animating = animator.IsAnimating(now);
        if (animating || wasAnimating)
        {
            panel.Invalidate();
        }

        wasAnimating = animating;
    }

    private void StartFirstLevel()
    {
        if (levelPaths.Count == 0)
        {
            MessageBox.Show(this, "No levels were found in the Levels folder.", "FireNice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadLevel(levelPaths[0]);
        clock.Start();
    }

    private void LoadLevel(string path)
    {
        LevelData data;
        try
        {
            data = LevelReader.Load(path);
        }
        catch (Exception ex) when (ex is LevelFormatException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Couldn't load {Path.GetFileName(path)}:\n\n{ex.Message}", "FireNice", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        CancelSolve();
        StopPlayback();
        if (game is not null)
        {
            game.StateChanged -= OnGameChanged;
        }

        game = new GameState(data);
        game.StateChanged += OnGameChanged;
        repeater = new InputRepeater(game, settings.RepeatIntervalMs);
        levelIndex = levelPaths.FindIndex(p => string.Equals(Path.GetFullPath(p), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
        nextTickAt = Now + settings.TickMs;
        animator.Sync(game, Now);
        panel.Game = game;
        UpdateStatusBar();
        clock.Start();
    }

    private void LoadLevelFromDialog()
    {
        repeater?.ReleaseAll();
        using var dialog = new OpenFileDialog
        {
            Title = "Load Level",
            Filter = "FireNice levels (*.txt)|*.txt|All files (*.*)|*.*",
            InitialDirectory = Path.Combine(AppContext.BaseDirectory, "Levels"),
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadLevel(dialog.FileName);
        }
    }

    private void ResetLevel()
    {
        if (game is null)
        {
            return;
        }

        CancelSolve();
        StopPlayback();
        repeater?.ReleaseAll();
        game.Reset();
        nextTickAt = Now + settings.TickMs;
    }

    /// <summary>Undoes the player's last move. Does nothing if there isn't one to undo.</summary>
    private void RewindOneStep()
    {
        if (game is null || !game.CanRewind)
        {
            return;
        }

        CancelSolve();
        StopPlayback();
        repeater?.ReleaseAll();
        game.Rewind();
        nextTickAt = Now + settings.TickMs;
    }

    /// <summary>
    /// Searches in the background for the fewest key presses that put out every fire from the
    /// game as it is now, then offers to play them back.
    /// </summary>
    private async void Solve()
    {
        if (game is null || solveCancel is not null)
        {
            return;
        }

        StopPlayback();
        repeater?.ReleaseAll();

        GameState solving = game;
        GameState snapshot = game.Clone();
        int run = ++solveRun;
        using var cancel = new CancellationTokenSource(settings.SolverTimeLimitMs);
        solveCancel = cancel;
        solveItem.Enabled = false;
        UseWaitCursor = true;
        UpdateStatusBar();

        SolveResult result;
        try
        {
            // The shortest search gets the first half of the time; the fallback gets the rest.
            var shortestLimit = TimeSpan.FromMilliseconds(settings.SolverTimeLimitMs / 2);
            result = await Task.Run(() => Solver.Solve(snapshot, cancel.Token, shortestLimit));
        }
        finally
        {
            if (run == solveRun)
            {
                solveCancel = null;
                solveItem.Enabled = true;
                UseWaitCursor = false;
                UpdateStatusBar();
            }
        }

        if (run != solveRun || !ReferenceEquals(solving, game))
        {
            return; // The level was reset or changed while solving.
        }

        switch (result.Outcome)
        {
            case SolveOutcome.Solved:
                if (MessageBox.Show(this, "Solution found!  Playback?", "Solve", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    StartPlayback(result.Inputs);
                }

                break;
            case SolveOutcome.SolvedNotShortest:
                if (MessageBox.Show(
                        this,
                        $"Couldn't find the shortest solution in time, but found one that takes {result.Inputs.Count} presses (there may be a shorter one).  Playback?",
                        "Solve",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    StartPlayback(result.Inputs);
                }

                break;
            case SolveOutcome.Impossible:
                MessageBox.Show(this, "Impossible to solve from here!", "Solve", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            case SolveOutcome.GaveUp:
                MessageBox.Show(this, "Couldn't find a solution in time.", "Solve", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
            case SolveOutcome.AlreadySolved:
                MessageBox.Show(this, "All the fires are already out!", "Solve", MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
        }
    }

    private void CancelSolve()
    {
        if (solveCancel is null)
        {
            return;
        }

        solveCancel.Cancel();
        solveCancel = null;
        solveRun++;
        solveItem.Enabled = true;
        UseWaitCursor = false;
    }

    /// <summary>Replays the solution with everything running at double speed.</summary>
    private void StartPlayback(IReadOnlyList<SolverInput> inputs)
    {
        if (game is null)
        {
            return;
        }

        repeater?.ReleaseAll();
        animator.WalkMs = settings.WalkAnimationMs / 2;
        animator.TickMs = settings.TickMs / 2;
        playback = new Playback(game, inputs, settings.TickMs / 2, settings.RepeatIntervalMs / 2, Now);
        UpdateStatusBar();
    }

    private void StopPlayback()
    {
        if (playback is null)
        {
            return;
        }

        playback = null;
        animator.WalkMs = settings.WalkAnimationMs;
        animator.TickMs = settings.TickMs;
        nextTickAt = Now + settings.TickMs;
        UpdateStatusBar();
    }

    private void NextLevel()
    {
        if (levelIndex >= 0 && levelIndex + 1 < levelPaths.Count)
        {
            LoadLevel(levelPaths[levelIndex + 1]);
        }
        else if (game?.Status == GameStatus.Won)
        {
            levelLabel.Text = "You've finished every level. Well done!";
        }
    }

    private void OnGameChanged(object? sender, EventArgs e)
    {
        if (game is not null)
        {
            animator.Sync(game, Now);
        }

        panel.Invalidate();
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        if (game is null)
        {
            return;
        }

        string position = levelIndex >= 0 ? $"Level {levelIndex + 1} of {levelPaths.Count}: " : string.Empty;
        string state = game.Status switch
        {
            GameStatus.Won => "  (complete)",
            GameStatus.Lost => "  (burned - press R to reset or Z to rewind)",
            _ => string.Empty,
        };
        if (solveCancel is not null)
        {
            state += "  (solving...)";
        }
        else if (playback is not null)
        {
            state += "  (playing the solution - press any key to stop)";
        }

        levelLabel.Text = position + game.Data.Name + state;
        firesLabel.Text = $"Fires left: {game.FiresLeft}";
        nextLevelItem.Enabled = levelIndex >= 0 && levelIndex + 1 < levelPaths.Count;
    }
}
