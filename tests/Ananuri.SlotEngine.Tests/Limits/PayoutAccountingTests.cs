using System.Text.Json;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Limits;

public sealed class PayoutAccountingTests
{
    [Theory]
    [InlineData(false, 10, 900, 1700, 100)]
    [InlineData(true, 10, 900, 2100, 100)]
    [InlineData(false, 17, 0, 1700, 1700)]
    [InlineData(true, 21, 0, 2100, 2100)]
    public void CombinedAwardsAtRoundCap_ShouldPreserveUncappedEvidenceAndStopMultiplierAdvancement(
        bool multiplyScatters, long winLimit, long previousRoundPayout, long expectedRawPayout, long expectedPayablePayout)
    {
        var game = Fixture(winLimit: winLimit, scatterPayout: true, multiplyScatter: multiplyScatters);
        var request = new SpinRequest("combined-cap", 100, Bonus(game, multiplier: 3, payout: previousRoundPayout));
        var draws = new Tape(0, 0, 0);

        var result = new SlotEngine().Evaluate(game, request, draws);

        var step = Assert.Single(result.Steps);
        var award = Assert.Single(step.Awards);
        Assert.Equal(500, award.Award.BasePayoutUnits);
        Assert.Equal(3, award.AppliedMultiplier);
        Assert.Equal(1500, award.RawPayoutUnits);
        Assert.Equal(200, step.Scatter.BasePayoutUnits);
        Assert.Equal(multiplyScatters ? 3 : 1, step.ScatterAppliedMultiplier);
        Assert.Equal(expectedRawPayout, step.RawPayoutUnits);
        Assert.Equal(expectedPayablePayout, step.PayablePayoutUnits);
        Assert.Equal(expectedRawPayout - expectedPayablePayout, step.CapAdjustmentUnits);
        Assert.Equal(expectedPayablePayout, result.TotalPayoutUnits);
        Assert.Equal(previousRoundPayout + expectedPayablePayout, result.RoundPayoutUnits);
        Assert.Equal(3, step.MultiplierBefore);
        Assert.Equal(3, step.MultiplierAfter);
        Assert.Null(step.Transition);
        Assert.Equal(CompletionReason.WinLimit, result.CompletionReason);
        Assert.Null(result.Continuation);
        Assert.Null(result.NextBonus);
        Assert.Equal(0, result.GrantedFreeSpins);
        Assert.Equal(3, draws.Used);
    }

    [Theory]
    [InlineData(false, 1700)]
    [InlineData(true, 2100)]
    public void ResumedCombinedAwards_ShouldApplyTheRemainingRoundAllowanceToTheNextGrid(
        bool multiplyScatters, long firstGridPayout)
    {
        var game = new GameDefinition("resumed-payout-accounting", "1", 3, [A, C, S],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip([S, A, A])),
            [new Payline(0, [2, 2, 2])], [new PaytableEntry(A, 3, 5)], new BetDefinition([100]),
            cascades: new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 3)
                .Select(_ => new WeightedSymbolTable([new(C, 1)])))),
            winLimit: new TotalStakeWinLimit(40),
            scatters: new GridScatterPolicy(S, [new(3, 2, 2)], [new(3, 2, 2)]),
            features: new FreeSpinsFeature(10),
            bonusMultiplier: new PayingCascadeMultiplier(1, 1, 100, MultiplierPersistence.Bonus),
            multiplyScatterAwards: multiplyScatters);
        var request = new SpinRequest("resumed-cap", 100, Bonus(game, multiplier: 3, payout: 900));
        var engine = new SlotEngine();
        var recording = new RecordingDrawSource(new Tape(0, 0, 0, 0, 0, 0));
        var whole = engine.Evaluate(game, request, recording);
        var replay = new ReplayDrawSource(recording.Snapshot());

        var partial = engine.Evaluate(game, request, replay, maxGridEvaluations: 1);
        var checkpoint = Assert.IsType<SpinContinuation>(partial.Continuation);
        Assert.Equal(firstGridPayout, partial.TotalPayoutUnits);
        Assert.Equal(900 + firstGridPayout, partial.RoundPayoutUnits);
        Assert.Equal(4, checkpoint.Multiplier);
        Assert.Equal(6, checkpoint.NextDrawOrdinal);
        var restored = SpinStateSerializer.DeserializeContinuation(SpinStateSerializer.Serialize(checkpoint));
        var completed = engine.Resume(game, restored, replay, maxGridEvaluations: 1);

        // The first grid advances 3x to 4x; the capped second grid must not advance it to 5x.
        var cappedStep = Assert.Single(completed.Steps);
        Assert.Equal(2000, cappedStep.RawPayoutUnits);
        Assert.Equal(3100 - firstGridPayout, cappedStep.PayablePayoutUnits);
        Assert.Equal(4, cappedStep.MultiplierBefore);
        Assert.Equal(4, cappedStep.AppliedMultiplier);
        Assert.Equal(4, cappedStep.MultiplierAfter);
        Assert.Null(cappedStep.Transition);
        Assert.Equal(3100, completed.TotalPayoutUnits);
        Assert.Equal(4000, completed.RoundPayoutUnits);
        Assert.Equal(CompletionReason.WinLimit, completed.CompletionReason);
        Assert.Null(completed.Continuation);
        Assert.Null(completed.NextBonus);
        Assert.Equal(0, completed.GrantedFreeSpins);
        Assert.Equal(6, recording.Snapshot().Length);
        Assert.Equal(JsonSerializer.Serialize(whole),
            JsonSerializer.Serialize(completed with { Steps = partial.Steps.AddRange(completed.Steps) }));
    }
}
