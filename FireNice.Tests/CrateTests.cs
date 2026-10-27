using FireNice.Core;
using static FireNice.Tests.TestLevel;

namespace FireNice.Tests;

public class CrateTests
{
    [Fact]
    public void PushedCrateMovesOneSpaceOnSolidGroundAndThePlayerStays()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PC...#",
            "#######");

        game.Press(Direction.Right);
        Assert.Equal(1, game.Player.X);
        RunUntilStill(game);

        Assert.Equal("#P.C..#", Rows(game)[2]);
    }

    [Fact]
    public void PushedCrateSlidesWhileTheGroundIsIce()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PC...#",
            "#IIIII#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P...C#", Rows(game)[2]);
    }

    [Fact]
    public void SlidingCrateStopsWhenItReachesGroundThatIsNotIce()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PC...#",
            "#III###",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P..C.#", Rows(game)[2]);
    }

    [Fact]
    public void SlidingCrateStopsAtIce()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PC..I#",
            "#IIIII#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P..CI#", Rows(game)[2]);
    }

    [Fact]
    public void TwoCratesSideBySideCannotBePushedSoThePlayerClimbs()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PCC..#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal((2, 1), (game.Player.X, game.Player.Y));
        Assert.Equal("#.CC..#", Rows(game)[2]);
    }

    [Fact]
    public void CratePushedOffALedgeFalls()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#PC..#",
            "###..#",
            "#....#",
            "######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#..C.#", Rows(game)[4]);
    }

    [Fact]
    public void CratePushedIntoFirePutsItOutAndRemains()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PCF..#",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P.C..#", Rows(game)[2]);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void CrateFallingOntoFirePutsItOutAndRemains()
    {
        GameState game = Game(
            "#####",
            "#.C.#",
            "#...#",
            "#.FP#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#.CP#", Rows(game)[3]);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void FireFallingOntoACrateSurvives()
    {
        GameState game = Game(
            "#####",
            "#.F.#",
            "#...#",
            "#.CP#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#.F.#", Rows(game)[2]);
        Assert.Equal(GameStatus.Playing, game.Status);
    }

    [Fact]
    public void CrateCanMoveOntoALitJarWithoutBeingDestroyed()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#PC...#",
            "###L###",
            "#######");

        game.Press(Direction.Right);
        RunUntilStill(game);

        Assert.Equal("#P.C..#", Rows(game)[2]);
        Assert.Equal(CellType.LitJar, game.Level[3, 3].Type);
    }

    [Fact]
    public void CrateFallingOntoALitJarIsNotDestroyed()
    {
        GameState game = Game(
            "#####",
            "#.C.#",
            "#...#",
            "#.L.#",
            "#P..#",
            "#####");

        RunUntilStill(game);

        Assert.Equal("#.C.#", Rows(game)[2]);
    }

    [Fact]
    public void CrateLandingOnThePlayerIsHarmless()
    {
        GameState game = Game(
            "#####",
            "#.C.#",
            "#.P.#",
            "#####");

        RunUntilStill(game);

        Assert.True(game.Player.IsAlive);
        Assert.Equal("#.C.#", Rows(game)[1]);
    }

    [Fact]
    public void ActionDoesNothingToACrate()
    {
        string[] rows =
        {
            "######",
            "#....#",
            "#P...#",
            "##C..#",
            "######",
        };
        GameState game = Game(rows);

        game.UseAction();

        AssertRows(game, rows);
    }

    [Fact]
    public void CreatedIceAttachesToACrate()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#P...#",
            "##.C.#",
            "######");

        game.UseAction();

        Assert.Equal("##=C.#", Rows(game)[3]);
    }

    [Fact]
    public void CrateHoldsAttachedIceUpOverAGap()
    {
        GameState game = Game(
            "######",
            "#....#",
            "#.P..#",
            "#.C..#",
            "#.#..#",
            "######");

        game.UseAction(); // Standing on the crate; the new ice attaches to it.
        RunUntilStill(game);

        Assert.Equal("#.C<.#", Rows(game)[3]);
    }

    [Theory]
    [InlineData('#')]
    [InlineData('J')]
    [InlineData('L')]
    [InlineData('|')]
    public void CrateAttachedToIceHeldByAFixedBlockDoesNotFallWhenItsSupportIsRemoved(char anchor)
    {
        GameState game = Game(
            "#######",
            "#.....#",
            $"#{anchor}=CP.#",
            "#..I###",
            "#..#..#",
            "#######");

        game.Press(Direction.Left); // Turn to face the ice under the crate...
        game.UseAction();           // ...and remove it.
        RunUntilStill(game);

        Assert.Equal(CellType.Air, game.Level[3, 3].Type);   // The ice under the crate is gone...
        Assert.Equal($"#{anchor}=CP.#", Rows(game)[2]);      // ...but the crate stays, still attached.
    }

    [Fact]
    public void CrateAndAttachedIceWithNothingHoldingThemFallTogetherAndStayAttached()
    {
        GameState game = Game(
            "######",
            "#.C<.#",
            "#....#",
            "#P...#",
            "######");

        RunUntilStill(game);

        AssertRows(game,
            "######",
            "#....#",
            "#....#",
            "#PC<.#",
            "######");
    }

    [Fact]
    public void CrateHangsFromAttachedIceThatIsStillRestingOnSomething()
    {
        string[] rows =
        {
            "######",
            "#.C<.#",
            "#..#.#",
            "#P...#",
            "######",
        };
        GameState game = Game(rows);

        RunUntilStill(game);

        AssertRows(game, rows);
    }

    [Fact]
    public void CrateWithIceAttachedCannotBePushed()
    {
        GameState game = Game(
            "#######",
            "#.....#",
            "#.P...#",
            "#.C...#",
            "####..#",
            "#######");

        game.UseAction();            // Ice attaches to the crate the player stands on: "C<".
        game.Press(Direction.Left);
        game.Press(Direction.Left);  // Step down beside the crate on the left.
        RunUntilStill(game);
        game.Press(Direction.Right);
        game.Press(Direction.Right); // Try to push it; the player climbs onto it instead.
        RunUntilStill(game);

        Assert.Equal("#.C<..#", Rows(game)[3]);
        Assert.Equal((2, 2), (game.Player.X, game.Player.Y));
    }

    [Fact]
    public void FallingPieceWithACrateLandsOnFireAndTheCratePutsItOut()
    {
        GameState game = Game(
            "######",
            "#.C<.#",
            "#....#",
            "#PF..#",
            "######");

        RunUntilStill(game);

        Assert.Equal("#PC<.#", Rows(game)[3]);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    [Fact]
    public void ResetPutsTheCrateBack()
    {
        string[] rows =
        {
            "#######",
            "#.....#",
            "#PC...#",
            "#######",
        };
        GameState game = Game(rows);
        game.Press(Direction.Right);
        RunUntilStill(game);

        game.Reset();

        AssertRows(game, rows);
    }
}
