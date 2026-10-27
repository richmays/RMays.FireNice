using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class IceActionTests
{
    private static GameState Ledge() => Game(
        "######",
        "#....#",
        "#P...#",
        "##...#",
        "#....#",
        "######");

    [Fact]
    public void IceCreatedOverAGapAttachesToTheBlockThePlayerStandsOn()
    {
        GameState game = Ledge();

        game.UseAction();
        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#P...#",
            "##<..#",
            "#....#",
            "######");
    }

    [Fact]
    public void IceCreatedBetweenTwoSolidsAttachesOnBothSides()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##.#.#",
            "#....#",
            "######");

        game.UseAction();

        Assert.Equal('=', CellChars.ToChar(game.Level[2, 3]));
    }

    [Fact]
    public void NewIceJoinsTheIceThePlayerStandsOn()
    {
        GameState game = Ledge();

        game.UseAction();
        game.Press(Direction.Right);
        game.UseAction();
        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#.P..#",
            "##=<.#",
            "#....#",
            "######");
    }

    [Fact]
    public void CreatingIceNextToAFreeCubeAttachesThem()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#.P..#",
            "#.I..#",
            "#.#..#",
            "######");

        game.UseAction();
        RunUntilStill(game);

        Assert.Equal("#.><.#", Rows(game)[3]);
    }

    [Fact]
    public void UsingTheActionOnIceRemovesItAndWhatWasOnTopFalls()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#PF..#",
            "##I..#",
            "###..#",
            "######");

        game.UseAction();
        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#P...#",
            "##F..#",
            "###..#",
            "######");
        Assert.True(game.Player.IsAlive);
    }

    [Fact]
    public void ActionTogglesIceOnAndOff()
    {
        GameState game = Ledge();

        game.UseAction();
        Assert.True(game.Level[2, 3].IsIce);

        game.UseAction();
        Assert.Equal(CellType.Air, game.Level[2, 3].Type);
    }

    [Fact]
    public void ActionOnABlockDoesNothing()
    {
        string[] rows =
        {
            "#####",
            "#...#",
            "#P..#",
            "#####",
        };
        GameState game = Game(rows);

        game.UseAction();

        AssertRows(game, rows);
    }

    [Fact]
    public void ActionOnFireDoesNothing()
    {
        string[] rows =
        {
            "#####",
            "#...#",
            "#P..#",
            "##F.#",
            "#####",
        };
        GameState game = Game(rows);

        game.UseAction();

        AssertRows(game, rows);
    }

    [Fact]
    public void AfterTurningTheActionTargetsTheNewSide()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#..P.#",
            "#..###",
            "#....#",
            "######");

        game.Press(Direction.Left);
        game.UseAction();

        Assert.Equal("#.>###", Rows(game)[3]);
    }
}
