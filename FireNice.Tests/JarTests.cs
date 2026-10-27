using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class JarTests
{
    [Fact]
    public void UnlitJarsActLikeBlocksForSlidingIce()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI...#",
            "#JJJJJ#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P...I#", Rows(game)[2]);
    }

    [Fact]
    public void PlayerCanClimbOntoAnUnlitJar()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PJ.#",
            "#####");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
        Assert.True(game.Player.IsAlive);
    }

    [Fact]
    public void CreatedIceAttachesToAJarAndIsHeldUp()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "#J...#",
            "#....#",
            "######");

        game.UseAction();
        RunUntilStill(game);

        Assert.Equal("#J<..#", Rows(game)[3]);
    }

    [Fact]
    public void JarsNeverFall()
    {
        string[] rows =
        {
            "#####",
            "#JL.#",
            "#...#",
            "#..P#",
            "#####",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void FireLandingOnAnUnlitJarLightsItAndKeepsBurning()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#...#",
            "#.J.#",
            "#P..#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#.F.#", Rows(game)[2]);
        Assert.Equal("#.L.#", Rows(game)[3]);
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void FireStartingOnAnUnlitJarLightsItOnTheFirstTick()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#.J.#",
            "#P..#",
            "#####");

        game.Tick();

        Assert.Equal(CellType.LitJar, game.Level[2, 2].Type);
    }

    [Fact]
    public void LitJarStaysLitAfterItsFireIsPutOut()
    {
        GameState game = Game(
            "#####",
            "#.I.#",
            "#...#",
            "#.F.#",
            "#.J.#",
            "#P..#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#...#", Rows(game)[3]);
        Assert.Equal("#.L.#", Rows(game)[4]);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void ClimbingOntoALitJarKillsThePlayer()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#PL.#",
            "#####");

        game.Press(Direction.Right);

        Assert.False(game.Player.IsAlive);
        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void FallingOntoALitJarKillsThePlayer()
    {
        GameState game = Game(
            "######",
            "#P...#",
            "##...#",
            "##L..#",
            "######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal(GameStatus.Lost, game.Status);
    }

    [Fact]
    public void StandingNextToALitJarIsSafe()
    {
        GameState game = Game(
            "#####",
            "#...#",
            "#P..#",
            "##L##",
            "#####");

        RunUntilStill(game);

        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void IcePushedOntoALitJarDisappears()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI...#",
            "###L###",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P....#", Rows(game)[2]);
        Assert.Equal(CellType.LitJar, game.Level[3, 3].Type);
    }

    [Fact]
    public void IceFallingOntoALitJarDisappears()
    {
        GameState game = Game(
            "#####",
            "#.I.#",
            "#...#",
            "#.L.#",
            "#P..#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#...#", Rows(game)[1]);
        Assert.Equal("#...#", Rows(game)[2]);
    }

    [Fact]
    public void OnlyTheCubeOnTheLitJarDisappearsAndItsNeighborIsDetached()
    {
        GameState game = Game(
            "######",
            "#.><.#",
            "#....#",
            "#.L#.#",
            "#P...#",
            "######");

        RunUntilStill(game);

        Assert.Equal("#..I.#", Rows(game)[2]);
    }

    [Fact]
    public void IceCreatedOnTopOfALitJarDisappearsAtOnce()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##...#",
            "##L..#",
            "######");

        game.UseAction();

        Assert.Equal("##...#", Rows(game)[3]);
    }

    [Fact]
    public void LitJarsDoNotStopTheLevelFromBeingWon()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PI.F.#",
            "#####L#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void ResetPutsALitJarBackToUnlit()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#.J.#",
            "#P..#",
            "#####");
        RunUntilStill(game);
        Assert.Equal(CellType.LitJar, game.Level[2, 2].Type);

        game.Reset();

        Assert.Equal(CellType.UnlitJar, game.Level[2, 2].Type);
    }
}
