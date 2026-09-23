using System.Collections.Immutable;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Simulator.Statistics;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Tests.Fixtures;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Simulator;

public sealed class GameStatisticsTests
{
    [Fact]
    public void CompleteRoundsProduceHandCalculatedTotalsFrequenciesBandsAndVariance()
    {
        var statistics = new GameStatistics();
        foreach (long payout in new long[] { 0, 50, 100, 200, 1000 })
        {
            var paid = Spin(SpinMode.Paid, payout, payout);
            statistics.ObservePaidSpin(paid);
            statistics.ObserveRound(paid, 0, 100);
        }

        var triggered = Spin(SpinMode.Paid, 500, 500, Bonus(500), granted: 2,
            steps: [Step(2, 3, refill: true), Step(3, 4)]);
        var retriggered = Spin(SpinMode.Free, 2000, 2500, Bonus(2500), granted: 3,
            steps: [Step(4, 5, refill: true), Step(5, 6)]);
        var capped = Spin(SpinMode.Free, 7500, 10000, completion: CompletionReason.WinLimit,
            steps: [Step(6, 7)]);
        statistics.ObservePaidSpin(triggered);
        statistics.ObserveFreeSpin(retriggered);
        statistics.ObserveFreeSpin(capped);
        statistics.ObserveRound(capped, 2, 100);

        Assert.Equal(6, statistics.CompletedRounds);
        Assert.Equal(600m, statistics.PaidStake);
        Assert.Equal(1850m, statistics.BasePayout);
        Assert.Equal(9500m, statistics.BonusPayout);
        Assert.Equal(2, statistics.FreeSpins);
        Assert.Equal(1, statistics.BonusRounds);
        Assert.Equal(1, statistics.Retriggers);
        Assert.Equal(1, statistics.CapHits);
        Assert.Equal(5, statistics.RoundHits);
        Assert.Equal(100m * 11350m / 600m, statistics.RtpPercent);
        Assert.Equal(100m * 1850m / 600m, statistics.BaseContributionPercent);
        Assert.Equal(100m * 9500m / 600m, statistics.BonusContributionPercent);
        Assert.Equal(100m * 1850m / 11350m, statistics.BasePayoutSharePercent);
        Assert.Equal(100m * 9500m / 11350m, statistics.BonusPayoutSharePercent);
        Assert.Equal(100m * 5m / 6m, statistics.RoundHitPercent);
        Assert.Equal(100m / 6m, statistics.BonusFrequencyPercent);
        Assert.Equal(2, statistics.Cascades);
        Assert.Equal(7, statistics.MaxMultiplier);
        Assert.Equal(10000, statistics.LargestRound);
        Assert.Equal(2, statistics.LongestBonus);
        Assert.Equal(new long[] { 1, 1, 1, 1, 1, 1 }, statistics.Bands);
        Assert.Equal(new Dictionary<int, long> { [0] = 6, [1] = 2 }, statistics.CascadeDistribution);
        Assert.Equal(new Dictionary<int, long> { [2] = 1 }, statistics.BonusLengths);
        Assert.Equal(new Dictionary<long, long> { [1] = 5, [2] = 1, [3] = 1, [4] = 1, [5] = 1, [6] = 1 }, statistics.MultiplierDistribution);
        // Round multiples: 0, 0.5, 1, 2, 10, 100. Sum=113.5, sum of squares=10105.25.
        Assert.Equal(113.5 / 6, statistics.Mean, 10);
        Assert.Equal(10105.25 - 113.5 * 113.5 / 6, statistics.M2, 9);
        Assert.Equal((10105.25 - 113.5 * 113.5 / 6) / 5, statistics.SampleVariance, 9);
    }

    [Fact]
    public void PayoutSharesAreZeroBeforeAnyPayout()
    {
        var statistics = new GameStatistics();
        Assert.Equal(0m, statistics.BasePayoutSharePercent);
        Assert.Equal(0m, statistics.BonusPayoutSharePercent);

        var paid = Spin(SpinMode.Paid, 0, 0);
        statistics.ObservePaidSpin(paid);
        statistics.ObserveRound(paid, 0, 100);

        Assert.Equal(0m, statistics.BasePayoutSharePercent);
        Assert.Equal(0m, statistics.BonusPayoutSharePercent);
    }

    [Fact]
    public void LargeIntegerAmountsDoNotRoundIntoTheNextPayoutBand()
    {
        const long stake = 1_000_000_000_000_000_000;
        var statistics = new GameStatistics();
        statistics.ObserveRound(Spin(SpinMode.Paid, stake - 1, stake - 1), 0, stake);
        Assert.Equal(new long[] { 0, 1, 0, 0, 0, 0 }, statistics.Bands);
    }

    [Fact]
    public void RejectsAnUnfinishedSpinAndAnUnfinishedBonusRound()
    {
        var statistics = new GameStatistics();
        var unfinished = Spin(SpinMode.Paid, 0, 0, completion: CompletionReason.WorkBudget);
        Assert.Throws<ArgumentException>(() => statistics.ObservePaidSpin(unfinished));
        Assert.Throws<ArgumentException>(() => statistics.ObserveFreeSpin(unfinished with { Mode = SpinMode.Free }));
        Assert.Throws<ArgumentException>(() => statistics.ObserveRound(unfinished, 0, 100));
        Assert.Throws<ArgumentException>(() => statistics.ObserveRound(Spin(SpinMode.Paid, 0, 0, Bonus(0)), 0, 100));
        Assert.Equal(0, statistics.CompletedRounds);
        Assert.Equal(0m, statistics.PaidStake);
    }

    [Fact]
    public void RejectsAChargedFreeSpinBeforeAddingItsPayout()
    {
        var statistics = new GameStatistics();
        var invalid = Spin(SpinMode.Free, 400, 400) with { ChargedStakeUnits = 100 };
        Assert.Throws<InvalidOperationException>(() => statistics.ObserveFreeSpin(invalid));
        Assert.Equal(0m, statistics.BonusPayout);
        Assert.Equal(0, statistics.FreeSpins);
    }

    private static BonusState Bonus(long payout) => new("round", "game", SlotEngine.EngineVersion,
        SlotEngine.RulesVersion, 100, 1, 2, 1, 1, payout);

    private static SpinEvaluation Spin(SpinMode mode, long payout, long roundPayout, BonusState? next = null,
        int granted = 0, CompletionReason completion = CompletionReason.NoMoreWins, ImmutableArray<CascadeStep> steps = default) =>
        new("spin", "game", "1", "fingerprint", SlotEngine.EngineVersion, SlotEngine.RulesVersion, mode,
            mode == SpinMode.Paid ? 100 : 0, 100, null, [0], steps.IsDefault ? [Step(1, 1)] : steps,
            payout, roundPayout, granted, next, completion, null);

    private static CascadeStep Step(long applied, long after, bool refill = false)
    {
        var board = EngineFixtures.Board(EngineFixtures.A);
        return new(0, board, [], ScatterResult.None, 1, applied, applied, after, [], 0, 0, 0,
            refill ? new CascadeTransition(board, [], [], [], 1) : null);
    }
}
