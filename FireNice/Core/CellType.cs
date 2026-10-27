namespace FireNice.Core;

public enum CellType
{
    Air,
    Block,
    Ice,
    Fire,

    /// <summary>Acts exactly like a block. Lights up if a fire is ever directly on top of it.</summary>
    UnlitJar,

    /// <summary>Solid like a block, but destroys ice and kills the player directly on top of it. Never goes out.</summary>
    LitJar,

    /// <summary>
    /// Iron crate: pushed and pulled by gravity like a single free ice cube, puts out fires, but is
    /// never destroyed. Only keeps sliding while the ground under it is ice.
    /// </summary>
    Crate,

    // Pipes never move. The player can travel through them; to everything else they're solid.
    PipeVertical,
    PipeHorizontal,
    PipeUpLeft,
    PipeUpRight,
    PipeDownLeft,
    PipeDownRight,
}
