using FireNice.Core;

namespace FireNice.UI;

/// <summary>
/// Shown while Solve runs: which search is running, how many presses deep it is, and how many
/// positions it has checked, refreshed once a second. Cancel (or Esc, or closing the window) asks to
/// stop the search.
/// </summary>
internal sealed class SolveProgressDialog : Form
{
    private readonly SolveProgress progress;
    private readonly Label stageLabel = new() { AutoSize = true };
    private readonly Label depthLabel = new() { AutoSize = true };
    private readonly Label positionsLabel = new() { AutoSize = true };
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 1000 };
    private bool closingFromCode;
    private bool closing;

    public SolveProgressDialog(SolveProgress progress)
    {
        this.progress = progress;
        Text = "Solving";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual; // Centered on the game window in OnLoad.
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var cancel = new Button { Text = "Cancel", AutoSize = true, Anchor = AnchorStyles.Right };
        cancel.Click += (_, _) => RequestCancel();
        CancelButton = cancel;

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
        };
        layout.Controls.Add(stageLabel);
        layout.Controls.Add(depthLabel);
        layout.Controls.Add(positionsLabel);
        layout.Controls.Add(cancel);
        foreach (Control label in new Control[] { stageLabel, depthLabel, positionsLabel })
        {
            label.Margin = new Padding(3, 3, 3, 6);
            label.MinimumSize = new Size(300, 0);
        }

        cancel.Margin = new Padding(3, 9, 3, 3);
        Controls.Add(layout);

        refresh.Tick += (_, _) => ShowProgress();
        ShowProgress();
        refresh.Start();
    }

    /// <summary>The player asked to stop the search.</summary>
    public event EventHandler? CancelRequested;

    /// <summary>Closes the dialog because the search is over, without asking to cancel it.</summary>
    public void CloseQuietly()
    {
        closingFromCode = true;
        refresh.Stop();
        if (!closing && !IsDisposed)
        {
            Close();
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // CenterParent only works for modal dialogs, and this one isn't modal.
        if (Owner is not null)
        {
            Location = new Point(
                Owner.Left + ((Owner.Width - Width) / 2),
                Owner.Top + ((Owner.Height - Height) / 2));
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing = true;
        if (!closingFromCode)
        {
            // The window's close button means the same as Cancel; it's closing already.
            closingFromCode = true;
            refresh.Stop();
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refresh.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RequestCancel()
    {
        if (closingFromCode)
        {
            return;
        }

        closingFromCode = true; // Only ask once.
        refresh.Stop();
        CancelRequested?.Invoke(this, EventArgs.Empty);
        if (!closing && !IsDisposed)
        {
            Close();
        }
    }

    private void ShowProgress()
    {
        stageLabel.Text = progress.Stage == SolveStage.Shortest
            ? "Looking for the shortest solution..."
            : "No shortest solution in time; looking for any solution...";
        depthLabel.Text = $"Search depth: {progress.Depth:N0} moves";
        positionsLabel.Text = $"Positions checked: {progress.PositionsChecked:N0}";
    }
}
