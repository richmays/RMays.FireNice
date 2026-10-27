namespace FireNice.Core;

/// <summary>
/// The live grid. (0, 0) is the top-left cell. Anything outside the grid reads as a Block.
/// </summary>
public sealed class Level
{
    private readonly Cell[,] cells;

    public Level(Cell[,] cells)
    {
        this.cells = cells;
        Width = cells.GetLength(0);
        Height = cells.GetLength(1);
    }

    public int Width { get; }

    public int Height { get; }

    public Cell this[int x, int y]
    {
        get => InBounds(x, y) ? cells[x, y] : Cell.Block;
        set
        {
            if (InBounds(x, y))
            {
                cells[x, y] = value;
            }
        }
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public Cell[,] CopyCells() => (Cell[,])cells.Clone();

    public int CountFires()
    {
        int count = 0;
        foreach (Cell cell in cells)
        {
            if (cell.Type == CellType.Fire)
            {
                count++;
            }
        }

        return count;
    }
}
