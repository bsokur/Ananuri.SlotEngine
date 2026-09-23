using System.Collections.Immutable;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Scatters;

public sealed class ScattersTests
{
    private readonly SlotEngine _engine = new();

    [Theory]
    [InlineData(false, 1700)]
    [InlineData(true, 2100)]
    public void ScatterPayout_ShouldUseConfiguredMultiplierEligibility(bool multiply, long expected)
    {
        var game = Fixture(scatterPayout: true, multiplyScatter: multiply);
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game, multiplier: 3)), new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(expected, result.TotalPayoutUnits);
        Assert.Equal(200, result.Steps[0].Scatter.BasePayoutUnits);
        Assert.Equal(4, result.NextBonus!.Multiplier);
    }

    [Fact]
    public void LosingSpin_ShouldTriggerBonusFromScattersWithoutAnOrdinaryAward()
    {
        var original = Fixture();
        var game = Compose(original, new NoCascades(), original.WinLimit,
            scatters: new GridScatterPolicy(S, [new(3, 2)]), features: new FreeSpinsFeature(10),
            bonusMultiplier: original.BonusMultiplier);
        // Stop 3 displays C,S: scatters on the payline have no ordinary award.
        var result = _engine.Evaluate(game, new("trigger", 100), new Tape(3, 3, 3));
        Assert.Equal(0, result.TotalPayoutUnits);
        Assert.Equal(2, result.GrantedFreeSpins);
        Assert.Equal(2, result.NextBonus!.RemainingSpins);
    }

    [Fact]
    public void ScatterCounting_ShouldSupportAtMostOnePerReelAndHighestThreshold()
    {
        var game = WildGame(rows: 2);
        var board = new SymbolBoard(5, 2, Enumerable.Range(0, 10).Select(i => new SymbolInstance(i, i < 4 ? S : C)));
        var all = new GridScatterPolicy(S, [new(3, 10), new(4, 12)]);
        var perReel = new GridScatterPolicy(S, [new(2, 5), new(3, 10)], atMostOnePerReel: true);
        var context = new ScatterContext(SpinMode.Paid, 0, board, 100, ImmutableHashSet<long>.Empty, false);
        Assert.Equal(12, all.Evaluate(game, context).RequestedFreeSpins);
        Assert.Equal(5, perReel.Evaluate(game, context).RequestedFreeSpins);
        Assert.Equal(0, all.Evaluate(game, context with { Mode = SpinMode.Free }).RequestedFreeSpins);
    }

    [Fact]
    public void ScatterNewArrivals_ShouldIgnoreSurvivors_AndHonorSingleTriggerPolicy()
    {
        var game = WildGame(rows: 2);
        var board = new SymbolBoard(5, 2, Enumerable.Range(0, 10).Select(i => new SymbolInstance(i, i < 6 ? S : C)));
        var policy = new GridScatterPolicy(S, [new(3, 10, 2)], timing: ScatterTiming.NewArrivalsEachGrid);
        var context = new ScatterContext(SpinMode.Paid, 1, board, 100, ImmutableHashSet.Create(0L, 1L, 2L), true);
        var result = policy.Evaluate(game, context);
        Assert.Equal(3, result.Count);
        Assert.Equal(new long[] { 3, 4, 5 }, result.CountedInstanceIds);
        Assert.Equal(0, result.RequestedFreeSpins);
        Assert.Equal(200, result.BasePayoutUnits);
        Assert.Equal(10, policy.Evaluate(game, context with { FeatureAlreadyTriggered = false }).RequestedFreeSpins);
        var initialOnly = new GridScatterPolicy(S, [new(3, 10)]);
        Assert.Equal(ScatterResult.None, initialOnly.Evaluate(game, context));
        var multiple = new GridScatterPolicy(S, [new(3, 10)], timing: ScatterTiming.NewArrivalsEachGrid, allowMultipleFeatureAwards: true);
        Assert.Equal(10, multiple.Evaluate(game, context).RequestedFreeSpins);
    }

    [Theory]
    [InlineData(ScatterTiming.InitialGrid, 0)]
    [InlineData(ScatterTiming.NewArrivalsEachGrid, 2)]
    public void RefillScatters_ShouldTriggerOnlyWhenSelectedPolicyPermitsIt(ScatterTiming timing, int expected)
    {
        var original = WildGame();
        var game = Compose(original,
            new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 5).Select(_ => new WeightedSymbolTable([new(S, 1)])))),
            new TotalStakeWinLimit(100), wins: original.Wins,
            scatters: new GridScatterPolicy(S, [new(3, 2)], timing: timing), features: new FreeSpinsFeature(10), allowScatterRefills: true);
        var result = new SlotEngine().Evaluate(game, new("id", 100), new Zeros());
        Assert.Equal(expected, result.GrantedFreeSpins);
        Assert.Equal(expected, result.NextBonus?.RemainingSpins ?? 0);
        Assert.Equal(1000, result.TotalPayoutUnits);
    }
}
