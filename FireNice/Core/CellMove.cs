namespace FireNice.Core;

/// <summary>Something in the grid (ice, fire or a crate) moved from one cell to another.</summary>
public readonly record struct CellMove(int FromX, int FromY, int ToX, int ToY);

/// <summary>What caused the most recent <see cref="GameState.StateChanged"/>.</summary>
public enum ChangeKind
{
    /// <summary>The level was (re)started; everything jumps to its place.</summary>
    Reset,

    /// <summary>The player pressed a key.</summary>
    Input,

    /// <summary>The game clock advanced: things fell, slid or moved through a pipe.</summary>
    Tick,
}
