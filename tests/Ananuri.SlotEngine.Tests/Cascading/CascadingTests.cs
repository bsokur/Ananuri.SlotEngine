using System.Collections.Immutable;
using System.Text.Json;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Cascading;

public sealed class CascadingTests
{
    private readonly SlotEngine _engine = new();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void WorkBudget_ShouldResumeWithIdenticalResultAndDrawOrder_AfterJsonRoundTrip(int budget)
    {
        var game = Fixture();
        var request = new SpinRequest("resume", 100, Bonus(game));
        var recording = new RecordingDrawSource(new Tape(1, 1, 1, 0, 1, 0, 0, 1, 0));
        var whole = _engine.Evaluate(game, request, recording);
        var replay = new ReplayDrawSource(recording.Snapshot());
        var part = _engine.Evaluate(game, request, replay, maxGridEvaluations: budget);
        Assert.False(part.IsComplete);
        Assert.Equal(CompletionReason.WorkBudget, part.CompletionReason);
        Assert.Null(part.NextBonus);
        var combined = part.Steps.ToList();
        while (part.Continuation is not null)
        {
            var checkpoint = SpinStateSerializer.DeserializeContinuation(SpinStateSerializer.Serialize(part.Continuation));
            part = _engine.Resume(game, checkpoint, replay, budget);
            combined.AddRange(part.Steps);
        }
        var resumed = part with { Steps = combined.ToImmutableArray() };
        Assert.Equal(JsonSerializer.Serialize(whole), JsonSerializer.Serialize(resumed));
    }

    [Fact]
    public async Task SharedDefinitionAndEngine_ShouldSupportIndependentConcurrentSpins()
    {
        var game = Fixture();
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() =>
            _engine.Evaluate(game, new($"id-{i}", 100), new Tape(0, 0, 0, 0, 1, 0)))));
        Assert.All(results, r => Assert.Equal(500, r.TotalPayoutUnits));
        Assert.Equal(32, results.Select(r => r.EvaluationId).Distinct().Count());
    }

    [Fact]
    public void OverlappingAwards_ShouldPayBoth_RemoveUnion_AndIncrementOnlyOnce()
    {
        var baseGame = new GameDefinition("overlap", "1", 2, [A, C, W],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip([W, A])),
            [new Payline(0, [0, 0, 0]), new Payline(1, [0, 1, 0])],
            [new PaytableEntry(A, 3, 5)], new BetDefinition([100]));
        var game = Compose(baseGame,
            new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 3).Select(_ => new WeightedSymbolTable([new(C, 1)])))),
            new TotalStakeWinLimit(100), wins: new PaylineWinEvaluator(new OrdinaryWildSubstitution([W], [A]), new HighestPayingAward()),
            paidMultiplier: new PayingCascadeMultiplier(1, 1, 10, MultiplierPersistence.Spin));
        var result = new SlotEngine().Evaluate(game, new("id", 100), new Zeros());
        Assert.Equal(2, result.Steps[0].Awards.Length);
        Assert.Equal(500, result.Steps[0].PayablePayoutUnits);
        Assert.Equal(2, result.Steps[0].MultiplierAfter);
        Assert.Equal(4, result.Steps[0].Transition!.Removed.Length);
        Assert.Equal(4, result.Steps[0].Transition!.Arrivals.Length);
        Assert.Equal(500, result.TotalPayoutUnits);
    }

    [Fact]
    public void NoCascadePolicy_ShouldPayOnceWithoutRefill()
    {
        var original = WildGame();
        var game = Compose(original, new NoCascades(), new TotalStakeWinLimit(100), wins: original.Wins);
        var recording = new RecordingDrawSource(new Zeros());
        var result = new SlotEngine().Evaluate(game, new("id", 100), recording);
        Assert.Equal(1000, result.TotalPayoutUnits);
        Assert.Single(result.Steps);
        Assert.Null(result.Steps[0].Transition);
        Assert.Equal(5, recording.Snapshot().Length);
    }
}
