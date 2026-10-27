using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class IceAttachmentTests
{
    [Fact]
    public void IceAttachedToABlockStaysUpOverAGap()
    {
        string[] rows =
        {
            "######",
            "#....#",
            "#P...#",
            "##<..#",
            "#....#",
            "######",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void PieceHeldByABlockAtOneEndStaysUp()
    {
        string[] rows =
        {
            "######",
            "#....#",
            "#P...#",
            "##=<.#",
            "#....#",
            "######",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void PieceNotHeldByABlockFallsAsOne()
    {
        GameState game = Game(
            "######",
            "#.><.#",
            "#....#",
            "#P...#",
            "######");

        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#....#",
            "#P><.#",
            "######");
    }

    [Fact]
    public void PieceWithOnlyOneCellSupportedStays()
    {
        string[] rows =
        {
            "######",
            "#....#",
            "#.><.#",
            "#.#..#",
            "#P...#",
            "######",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void PieceOverIceAndFireStays()
    {
        string[] rows =
        {
            "######",
            "#....#",
            "#.><.#",
            "#PIF.#",
            "######",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void PieceOverAirAndFireFallsAndTheCubeOnTheFireMeltsWithIt()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#.><.#",
            "#P.F.#",
            "######");

        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#....#",
            "#PI..#",
            "######");
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void FallingIceDoesNotAttachToWhatItLandsBeside()
    {
        GameState game = Game(
            "#####",
            "#I..#",
            "#...#",
            "#..P#",
            "#####");

        RunUntilStill(game);

        AssertRows(game,
            "#####",
            "#...#",
            "#...#",
            "#I.P#",
            "#####");
    }

    [Fact]
    public void RemovingCubesFromABridgeSplitsItAndUnheldPartsFall()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#..P..#",
            "##===##",
            "#.....#",
            "#######");

        game.UseAction();
        RunUntilStill(game);
        AssertRows(game,
            "#######",
            "#.....#",
            "#..P..#",
            "##=<.##",
            "#.....#",
            "#######");

        game.Press(Direction.Left);
        game.UseAction();
        RunUntilStill(game);
        AssertRows(game,
            "#######",
            "#.....#",
            "#.....#",
            "##.P.##",
            "#..I..#",
            "#######");
    }
}
