namespace FireNice.Core;

/// <summary>
/// One grid cell. The attachment flags are only meaningful for ice.
/// </summary>
public readonly record struct Cell(CellType Type, bool AttachedLeft = false, bool AttachedRight = false)
{
    public static readonly Cell Air = new(CellType.Air);
    public static readonly Cell Block = new(CellType.Block);
    public static readonly Cell Fire = new(CellType.Fire);
    public static readonly Cell UnlitJar = new(CellType.UnlitJar);
    public static readonly Cell LitJar = new(CellType.LitJar);
    public static readonly Cell Crate = new(CellType.Crate);

    public static Cell Ice(bool attachedLeft = false, bool attachedRight = false) =>
        new(CellType.Ice, attachedLeft, attachedRight);

    public bool IsIce => Type == CellType.Ice;

    /// <summary>Ice with no attachments; the only kind of ice that can be pushed.</summary>
    public bool IsFreeIce => IsIce && !AttachedLeft && !AttachedRight;

    /// <summary>Things the player can push: free ice and iron crates.</summary>
    public bool IsPushable => IsFreeIce || Type == CellType.Crate;

    /// <summary>Blocks, jars and pipes: solid, and they never move. Ice can attach to them.</summary>
    public bool IsFixed => Type is CellType.Block or CellType.UnlitJar or CellType.LitJar || IsPipe;

    /// <summary>
    /// Created ice attaches to this: blocks, jars, pipes and iron crates. Fixed things hold the
    /// ice up; a crate instead joins the ice's piece and moves with it.
    /// </summary>
    public bool IsAnchor => IsFixed || Type == CellType.Crate;

    public bool IsPipe => PipeOpenings != Openings.None;

    /// <summary>The two open sides of a pipe, or None for anything else.</summary>
    public Openings PipeOpenings => Type switch
    {
        CellType.PipeVertical => Openings.Up | Openings.Down,
        CellType.PipeHorizontal => Openings.Left | Openings.Right,
        CellType.PipeUpLeft => Openings.Up | Openings.Left,
        CellType.PipeUpRight => Openings.Up | Openings.Right,
        CellType.PipeDownLeft => Openings.Down | Openings.Left,
        CellType.PipeDownRight => Openings.Down | Openings.Right,
        _ => Openings.None,
    };

    public bool IsOpen(Heading side) => PipeOpenings.HasFlag(side.ToOpening());

    /// <summary>Air and fire don't hold anything up.</summary>
    public bool IsEmpty => Type is CellType.Air or CellType.Fire;
}
