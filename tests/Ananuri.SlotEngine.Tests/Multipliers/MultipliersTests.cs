using System.Collections.Immutable;
using System.Text.Json;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Multipliers;

public sealed class MultipliersTests
{
    private readonly SlotEngine _engine = new();

    [Fact]
    public void SpinPersistence_ShouldResetBetweenFreeSpins()
    {
        var game = Fixture(bonus: new PayingCascadeMultiplier(1, 1, 20, MultiplierPersistence.Spin));
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game)), new Tape(1, 1, 1, 0, 1, 0, 0, 1, 0));
        Assert.Equal(3, result.Steps[^1].MultiplierAfter);
        Assert.Equal(1, result.NextBonus!.Multiplier);
    }

    [Fact]
    public void CollectedSymbol_ShouldCollectBeforeAward_AndNeverCollectFallingInstanceTwice()
    {
        var strategy = new CollectedSymbolMultiplier(1, 50, MultiplierPersistence.Bonus, [new(M, 2)]);
        var game = Fixture(bonus: strategy, includeMultiplierSymbol: true);
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game)), new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(3500, result.TotalPayoutUnits);
        Assert.Equal(7, result.Steps[0].AppliedMultiplier);
        Assert.Equal(3, result.Steps[0].Collections.Length);
        Assert.Empty(result.Steps[1].Collections);
        Assert.Equal(7, result.NextBonus!.Multiplier);
        Assert.Equal(result.Steps[0].Collections.Select(c => c.InstanceId).Order(),
            result.Steps[1].Grid.Cells.Where(c => c.Symbol == M).Select(c => c.Id).Order());
    }

    [Theory]
    [InlineData(false, false, 3)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 3)]
    public void Collection_ShouldRespectWinRequirementAndTiming(bool requiresWin, bool before, long expectedNext)
    {
        var strategy = new CollectedSymbolMultiplier(1, 3, MultiplierPersistence.Spin, [new(M, long.MaxValue)], before, requiresWin);
        var board = new SymbolBoard(1, 1, new[] { new SymbolInstance(0, M) });
        var result = strategy.Apply(new(1), new(SpinMode.Paid, 0, board, [], ImmutableHashSet<long>.Empty));
        Assert.Equal(expectedNext, result.NextState.Value);
        Assert.Equal(before ? expectedNext : 1, result.AppliedMultiplier);
    }

    [Fact]
    public void PayingMultiplier_ShouldRespectInitialGridOptionAndMaximum()
    {
        var strategy = new PayingCascadeMultiplier(1, long.MaxValue, 4, MultiplierPersistence.Spin, includeInitialGrid: false);
        var board = new SymbolBoard(1, 1, new[] { new SymbolInstance(0, A) });
        var context = new MultiplierContext(SpinMode.Paid, 0, board,
            [new WinAward(0, A, 1, 1, 100, [new(0, 0)], [])], ImmutableHashSet<long>.Empty);
        Assert.Equal(1, strategy.Apply(new(1), context).NextState.Value);
        var next = strategy.Apply(new(1), context with { GridIndex = 1 });
        Assert.Equal(1, next.AppliedMultiplier);
        Assert.Equal(4, next.NextState.Value);
        Assert.Equal(4, strategy.Apply(next.NextState, context with { GridIndex = 2 }).NextState.Value);
    }

    [Fact]
    public void CollectedState_ShouldSurviveCheckpoint_AndAllowNewInstancesInNextSpin()
    {
        var strategy = new CollectedSymbolMultiplier(1, 50, MultiplierPersistence.Bonus, [new(M, 2)]);
        var game = Fixture(bonus: strategy, includeMultiplierSymbol: true);
        var request = new SpinRequest("first", 100, Bonus(game));
        var recording = new RecordingDrawSource(new Tape(0, 0, 0, 0, 1, 0));
        var part = _engine.Evaluate(game, request, recording, 1);
        var checkpoint = JsonSerializer.Deserialize<SpinContinuation>(JsonSerializer.Serialize(part.Continuation))!;
        Assert.Equal(3, checkpoint.CollectedInstanceIds.Count);
        var complete = _engine.Resume(game, checkpoint, new ReplayDrawSource(recording.Snapshot()));
        Assert.Empty(Assert.Single(complete.Steps).Collections);
        Assert.Equal(3500, complete.TotalPayoutUnits);
        var next = _engine.Evaluate(game, new("second", 100, complete.NextBonus), new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(13, next.Steps[0].AppliedMultiplier);
        Assert.Equal(3, next.Steps[0].Collections.Length);
        Assert.Equal(6500, next.TotalPayoutUnits);
    }
}
