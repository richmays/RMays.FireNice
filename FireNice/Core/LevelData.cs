namespace FireNice.Core;

/// <summary>
/// An untouched copy of a level as it was loaded. Reset rebuilds the game from this.
/// </summary>
public sealed class LevelData
{
    private readonly Cell[,] cells;

    public LevelData(string name, Cell[,] cells, int playerStartX, int playerStartY)
    {
        Name = name;
        this.cells = (Cell[,])cells.Clone();
        PlayerStartX = playerStartX;
        PlayerStartY = playerStartY;
    }

    public string Name { get; }

    public int Width => cells.GetLength(0);

    public int Height => cells.GetLength(1);

    public int PlayerStartX { get; }

    public int PlayerStartY { get; }

    public Cell this[int x, int y] => cells[x, y];

    public Cell[,] CopyCells() => (Cell[,])cells.Clone();
}
