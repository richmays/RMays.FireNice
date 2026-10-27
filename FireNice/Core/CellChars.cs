namespace FireNice.Core;

/// <summary>
/// The one-character-per-cell alphabet used by level files.
/// </summary>
public static class CellChars
{
    public static char ToChar(Cell cell) => cell.Type switch
    {
        CellType.Air => '.',
        CellType.Block => '#',
        CellType.Fire => 'F',
        CellType.UnlitJar => 'J',
        CellType.LitJar => 'L',
        CellType.Crate => 'C',
        CellType.PipeVertical => '|',
        CellType.PipeHorizontal => '-',
        CellType.PipeUpLeft => '7',
        CellType.PipeUpRight => '9',
        CellType.PipeDownLeft => '1',
        CellType.PipeDownRight => '3',
        CellType.Ice => (cell.AttachedLeft, cell.AttachedRight) switch
        {
            (false, false) => 'I',
            (true, false) => '<',
            (false, true) => '>',
            (true, true) => '=',
        },
        _ => '?',
    };

    public static Cell? FromChar(char ch) => ch switch
    {
        '.' => Cell.Air,
        '#' => Cell.Block,
        'F' => Cell.Fire,
        'J' => Cell.UnlitJar,
        'L' => Cell.LitJar,
        'C' => Cell.Crate,

        // Bends use the number pad: the digit sits in the corner where the two openings meet.
        '|' => new Cell(CellType.PipeVertical),
        '-' => new Cell(CellType.PipeHorizontal),
        '7' => new Cell(CellType.PipeUpLeft),
        '9' => new Cell(CellType.PipeUpRight),
        '1' => new Cell(CellType.PipeDownLeft),
        '3' => new Cell(CellType.PipeDownRight),
        'I' => Cell.Ice(),
        '<' => Cell.Ice(attachedLeft: true),
        '>' => Cell.Ice(attachedRight: true),
        '=' => Cell.Ice(attachedLeft: true, attachedRight: true),
        _ => null,
    };
}
