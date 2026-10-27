namespace FireNice.Core;

/// <summary>One of the four ways to move through a pipe.</summary>
public enum Heading
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>Which sides of a pipe are open.</summary>
[Flags]
public enum Openings
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
}

public static class HeadingExtensions
{
    public static int Dx(this Heading heading) => heading switch
    {
        Heading.Left => -1,
        Heading.Right => 1,
        _ => 0,
    };

    public static int Dy(this Heading heading) => heading switch
    {
        Heading.Up => -1,
        Heading.Down => 1,
        _ => 0,
    };

    public static Heading Opposite(this Heading heading) => heading switch
    {
        Heading.Up => Heading.Down,
        Heading.Down => Heading.Up,
        Heading.Left => Heading.Right,
        _ => Heading.Left,
    };

    public static Openings ToOpening(this Heading heading) => heading switch
    {
        Heading.Up => Openings.Up,
        Heading.Down => Openings.Down,
        Heading.Left => Openings.Left,
        _ => Openings.Right,
    };

    public static Heading ToHeading(this Direction direction) =>
        direction == Direction.Left ? Heading.Left : Heading.Right;

    public static bool IsHorizontal(this Heading heading) => heading is Heading.Left or Heading.Right;
}
