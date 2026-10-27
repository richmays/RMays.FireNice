namespace FireNice.Core;

public enum Direction
{
    Left,
    Right,
}

public static class DirectionExtensions
{
    /// <summary>Horizontal step for this direction: -1 for left, +1 for right.</summary>
    public static int Dx(this Direction direction) => direction == Direction.Left ? -1 : 1;
}
