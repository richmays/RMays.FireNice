namespace FireNice.Core;

public sealed class Player
{
    public Player(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; internal set; }

    public int Y { get; internal set; }

    /// <summary>Every level starts with the player facing right.</summary>
    public Direction Facing { get; internal set; } = Direction.Right;

    public bool IsAlive { get; internal set; } = true;

    internal Player Clone() => new(X, Y) { Facing = Facing, IsAlive = IsAlive };
}
