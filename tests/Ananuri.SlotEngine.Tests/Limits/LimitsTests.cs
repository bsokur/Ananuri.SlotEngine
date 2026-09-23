using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Limits;

public sealed class LimitsTests
{
    private readonly SlotEngine _engine = new();

    [Theory]
    [InlineData(false, CompletionReason.NoMoreWins)]
    [InlineData(true, CompletionReason.WinLimit)]
    public void MaximumRepresentablePayout_ShouldDistinguishNoLimitFromAnExactCap(bool capped, CompletionReason completion)
    {
        var game = new GameDefinition("maximum-payout", "1", 1, [A], [new ReelStrip([A])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, long.MaxValue)], new BetDefinition([1]),
            winLimit: capped ? new TotalStakeWinLimit(long.MaxValue) : null);

        var result = _engine.Evaluate(game, new("maximum", 1), new FixedReelStops(game, [0]));

        Assert.True(result.IsComplete);
        Assert.Equal(long.MaxValue, result.TotalPayoutUnits);
        Assert.Equal(long.MaxValue, result.RoundPayoutUnits);
        Assert.Equal(completion, result.CompletionReason);
        Assert.Equal(0, Assert.Single(result.Steps).CapAdjustmentUnits);
        Assert.Null(result.Continuation);
    }

    [Theory]
    [InlineData(null, 500, CompletionReason.NoMoreWins)]
    [InlineData(250L, 250, CompletionReason.WinLimit)]
    [InlineData(500L, 500, CompletionReason.WinLimit)]
    [InlineData(501L, 500, CompletionReason.NoMoreWins)]
    public void CustomLimit_ShouldApplyNullablePayoutContract(long? maximum, long expected, CompletionReason completion)
    {
        var game = new GameDefinition("custom-limit", "1", 1, [A], [new ReelStrip([A])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 5)], new BetDefinition([100]),
            winLimit: new FixedLimit(maximum));
        var result = _engine.Evaluate(game, new("custom-limit", 100), new FixedReelStops(game, [0]));
        Assert.Equal(expected, result.TotalPayoutUnits);
        Assert.Equal(completion, result.CompletionReason);
        Assert.Equal(500 - expected, Assert.Single(result.Steps).CapAdjustmentUnits);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CustomLimit_ShouldRejectNonpositiveCapAtConstruction(long maximum) =>
        Assert.Throws<ArgumentException>(() => new GameDefinition("invalid-limit", "1", 1,
            [A], [new ReelStrip([A])], [new Payline(0, [0])], [new PaytableEntry(A, 1, 1)],
            new BetDefinition([100]), winLimit: new FixedLimit(maximum)));

    [Fact]
    public void UncappedRoundAtMaximumPayout_ShouldAllowRemainingLosingFreeSpins()
    {
        var game = new GameDefinition("uncapped-bonus", "1", 1, [A, B], [new ReelStrip([B])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 1)], new BetDefinition([100]),
            features: new FreeSpinsFeature(10));
        var request = new SpinRequest("maximum-round", 100, Bonus(game, remaining: 2, payout: long.MaxValue));
        var result = _engine.Evaluate(game, request, new FixedReelStops(game, [0], SpinMode.Free));
        Assert.Equal(0, result.TotalPayoutUnits);
        Assert.Equal(long.MaxValue, result.RoundPayoutUnits);
        Assert.Equal(CompletionReason.NoMoreWins, result.CompletionReason);
        Assert.Equal(1, result.NextBonus!.RemainingSpins);
        Assert.Equal(long.MaxValue, result.NextBonus.RoundPayoutUnits);

        var last = _engine.Evaluate(game, new("last-free", 100, result.NextBonus), new FixedReelStops(game, [0], SpinMode.Free));
        Assert.Equal(long.MaxValue, last.RoundPayoutUnits);
        Assert.Null(last.NextBonus);
    }

    [Fact]
    public void UncappedRoundOverflow_ShouldFailInsteadOfTruncatingTheAward()
    {
        var game = new GameDefinition("uncapped-overflow", "1", 1, [A], [new ReelStrip([A])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 1)], new BetDefinition([100]),
            features: new FreeSpinsFeature(10));
        var request = new SpinRequest("overflow", 100, Bonus(game, payout: long.MaxValue));
        Assert.Throws<OverflowException>(() => _engine.Evaluate(game, request, new FixedReelStops(game, [0], SpinMode.Free)));
    }

    [Fact]
    public void RoundCap_ShouldIncludePriorBonusPayout_AndStopWithoutExtraDrawsOrRetrigger()
    {
        var game = Fixture(winLimit: 10);
        var tape = new Tape(0, 0, 0);
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game, multiplier: 3, payout: 900)), tape);
        Assert.Equal(100, result.TotalPayoutUnits);
        Assert.Equal(1000, result.RoundPayoutUnits);
        Assert.Equal(1500, result.Steps[0].RawPayoutUnits);
        Assert.Equal(1400, result.Steps[0].CapAdjustmentUnits);
        Assert.Equal(CompletionReason.WinLimit, result.CompletionReason);
        Assert.Null(result.Steps[0].Transition);
        Assert.Null(result.NextBonus);
        Assert.Equal(0, result.GrantedFreeSpins);
        Assert.Equal(3, tape.Used);
    }

    [Fact]
    public void CappedCascadeDistribution_ShouldMatchIndependentGeometricCalculation()
    {
        // Each grid independently wins with probability 1/4. Pay 1x per win, stop at 3x.
        // P(0)=3/4, P(1)=3/16, P(2)=3/64, P(3)=1/64.
        var baseGame = new GameDefinition("geometric", "1", 1, [A, C],
            [new ReelStrip([A, C]), new ReelStrip([A, C])], [new Payline(0, [0, 0])],
            [new PaytableEntry(A, 2, 1)], new BetDefinition([100]));
        var table = new WeightedSymbolTable([new(A, 1), new(C, 1)]);
        var game = Compose(baseGame,
            new FallingSymbolsCascade(new WeightedSymbolRefill([table, table])), new TotalStakeWinLimit(3));
        var engine = new SlotEngine();
        var distribution = new Dictionary<long, int>();
        for (int value = 0; value < 64; value++)
        {
            var result = engine.Evaluate(game, new($"id-{value}", 100), new BitDraws(value));
            distribution[result.TotalPayoutUnits] = distribution.GetValueOrDefault(result.TotalPayoutUnits) + 1;
        }
        Assert.Equal(new[] { (0L, 48), (100L, 12), (200L, 3), (300L, 1) },
            distribution.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
        Assert.Equal(2100, distribution.Sum(p => p.Key * p.Value));
    }

    private sealed class FixedLimit(long? maximum) : IWinLimitPolicy
    {
        public string ConfigurationKey => $"test-fixed-limit:{maximum?.ToString() ?? "none"}";
        public long? MaximumPayout(long calculationStake) => maximum;
        public void Validate(GameDefinition game) { }
    }
}
