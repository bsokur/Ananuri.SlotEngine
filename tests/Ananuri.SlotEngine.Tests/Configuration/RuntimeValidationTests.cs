using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class RuntimeValidationTests
{
    [Fact]
    public void Resume_ShouldValidateRequestBeforeCheckpointAndDraws()
    {
        var game = Fixture();
        var state = Checkpoint(game) with
        {
            Request = new("unsupported-stake", 101),
            GameFingerprint = "another-game"
        };
        var draws = new Tape();

        var error = Assert.Throws<ArgumentException>(() => new SlotEngine().Resume(game, state, draws));

        Assert.Equal("Unsupported calculation stake.", error.Message);
        Assert.Equal(0, draws.Used);
    }

    [Theory]
    [InlineData("rules")]
    [InlineData("grid-index")]
    [InlineData("draw-ordinal")]
    [InlineData("low-multiplier")]
    [InlineData("high-multiplier")]
    [InlineData("spin-payout")]
    [InlineData("round-payout")]
    [InlineData("payout-mismatch")]
    [InlineData("round-cap")]
    [InlineData("pending-spins")]
    [InlineData("missing-collected-ids")]
    [InlineData("negative-collected-id")]
    [InlineData("future-collected-id")]
    [InlineData("missing-counted-ids")]
    [InlineData("negative-counted-id")]
    [InlineData("future-counted-id")]
    [InlineData("uninitialized-stops")]
    [InlineData("missing-stops")]
    public void Resume_ShouldRejectInvalidCheckpointBeforeDraws(string fault)
    {
        var game = Fixture();
        var state = Checkpoint(game);
        state = fault switch
        {
            "rules" => state with { RulesVersion = "other-rules" },
            "grid-index" => state with { NextGridIndex = -1 },
            "draw-ordinal" => state with { NextDrawOrdinal = -1 },
            "low-multiplier" => state with { Multiplier = 0 },
            "high-multiplier" => state with { Multiplier = game.PaidMultiplier.Maximum + 1 },
            "spin-payout" => state with { SpinPayoutUnits = -1 },
            "round-payout" => state with { RoundPayoutUnits = -1 },
            "payout-mismatch" => state with { RoundPayoutUnits = state.SpinPayoutUnits + 1 },
            "round-cap" => state with { SpinPayoutUnits = 100_000, RoundPayoutUnits = 100_000 },
            "pending-spins" => state with { PendingFreeSpins = -1 },
            "missing-collected-ids" => state with { CollectedInstanceIds = null! },
            "negative-collected-id" => state with { CollectedInstanceIds = [-1] },
            "future-collected-id" => state with { CollectedInstanceIds = [state.NextInstanceId] },
            "missing-counted-ids" => state with { CountedScatterInstanceIds = null! },
            "negative-counted-id" => state with { CountedScatterInstanceIds = [-1] },
            "future-counted-id" => state with { CountedScatterInstanceIds = [state.NextInstanceId] },
            "uninitialized-stops" => state with { InitialStops = default },
            "missing-stops" => state with { InitialStops = [] },
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };
        var draws = new Tape();

        var error = Assert.Throws<ArgumentException>(() => new SlotEngine().Resume(game, state, draws));

        Assert.Equal("Invalid continuation or package/engine mismatch.", error.Message);
        Assert.Equal(0, draws.Used);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Resume_ShouldRejectOutOfRangeReelStopBeforeDraws(int stop)
    {
        var game = Fixture();
        var state = Checkpoint(game) with { InitialStops = [stop, 0, 0] };
        var draws = new Tape();

        var error = Assert.Throws<ArgumentException>(() => new SlotEngine().Resume(game, state, draws));

        Assert.Equal("Invalid continuation reel stop.", error.Message);
        Assert.Equal(0, draws.Used);
    }

    [Theory]
    [InlineData("dimensions")]
    [InlineData("symbol")]
    [InlineData("instance-id")]
    public void Resume_ShouldRejectInvalidBoardBeforeDraws(string fault)
    {
        var game = Fixture();
        var state = Checkpoint(game);
        state = fault switch
        {
            "dimensions" => state with { Grid = Board(A) },
            "symbol" => state with
            {
                Grid = new SymbolBoard(game.ReelCount, game.VisibleRows,
                    state.Grid.Cells.Select(cell => cell with { Symbol = new SymbolId(99) }))
            },
            "instance-id" => state with { NextInstanceId = state.Grid.Cells.Max(cell => cell.Id) },
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };
        var draws = new Tape();

        var error = Assert.Throws<ArgumentException>(() => new SlotEngine().Resume(game, state, draws));

        Assert.Equal("Invalid grid dimensions, symbols, or instance sequence.", error.Message);
        Assert.Equal(0, draws.Used);
    }

    private static SpinContinuation Checkpoint(GameDefinition game) =>
        Assert.IsType<SpinContinuation>(new SlotEngine().Evaluate(game, new("checkpoint", 100),
            new Tape(0, 0, 0, 0, 0, 0), maxGridEvaluations: 1).Continuation);
}
