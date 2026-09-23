using System.Text.Json;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Cascading;

public sealed class CheckpointBoundaryTests
{
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 2)]
    [InlineData(false, true, 2)]
    public void ResumedScatterBatches_ShouldPreservePayoutsGrantsAndCapTermination(bool capped, bool repeatGrants, int budget)
    {
        var game = new GameDefinition("checkpoint-boundaries", "1", 3, [A, C, S],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip([S, A, A])),
            [new Payline(0, [2, 2, 2])], [new PaytableEntry(A, 3, 5)], new BetDefinition([100]),
            cascades: new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 3)
                .Select(_ => new WeightedSymbolTable([new(S, 1), new(C, 1)])))),
            winLimit: capped ? new TotalStakeWinLimit(9) : new NoWinLimit(),
            scatters: new GridScatterPolicy(S, [new(3, 2, 1)], timing: ScatterTiming.NewArrivalsEachGrid,
                allowMultipleFeatureAwards: repeatGrants),
            features: new FreeSpinsFeature(10), allowScatterRefills: true);
        var engine = new SlotEngine();
        var request = new SpinRequest("boundary", 100);
        var recording = new RecordingDrawSource(new Tape(0, 0, 0, 0, 0, 0, 1, 1, 1));
        var whole = engine.Evaluate(game, request, recording);

        // Two independent scatter batches each pay 100, alongside two 500-unit line awards.
        Assert.Equal(capped ? 900 : 1200, whole.TotalPayoutUnits);
        Assert.Equal(capped ? 2 : 3, whole.Steps.Length);
        Assert.Equal(capped ? 0 : repeatGrants ? 4 : 2, whole.GrantedFreeSpins);
        Assert.Equal(capped ? CompletionReason.WinLimit : CompletionReason.NoMoreWins, whole.CompletionReason);
        Assert.Equal(capped ? 6 : 9, recording.Snapshot().Length);
        if (capped) Assert.Null(whole.NextBonus);
        else Assert.Equal(whole.GrantedFreeSpins, whole.NextBonus!.RemainingSpins);

        var replay = new ReplayDrawSource(recording.Snapshot());
        var part = engine.Evaluate(game, request, replay, budget);
        Assert.NotNull(part.Continuation);
        var steps = part.Steps.ToBuilder();
        while (part.Continuation is not null)
        {
            var state = SpinStateSerializer.DeserializeContinuation(SpinStateSerializer.Serialize(part.Continuation));
            part = engine.Resume(game, state, replay, budget);
            steps.AddRange(part.Steps);
        }
        Assert.Equal(JsonSerializer.Serialize(whole), JsonSerializer.Serialize(part with { Steps = steps.ToImmutable() }));
    }
}
