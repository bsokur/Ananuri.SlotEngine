using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Grid;

public sealed class CascadeContractTests
{
    [Theory]
    [InlineData("retained removed cells")]
    [InlineData("changed survivor")]
    [InlineData("lost survivor")]
    [InlineData("missing movement")]
    [InlineData("wrong movement")]
    [InlineData("duplicate movement")]
    [InlineData("missing arrival")]
    [InlineData("wrong arrival")]
    [InlineData("duplicate arrival")]
    [InlineData("skipped instance id")]
    [InlineData("wrong removals")]
    [InlineData("uninitialized evidence")]
    public void FaultyCascade_ShouldFailBeforeTheNextGrid(string fault)
    {
        var original = Fixture();
        var policy = new ChangedTransition(original.Cascades, (before, transition) => fault switch
        {
            "retained removed cells" => transition with { Grid = before },
            "changed survivor" => transition with { Grid = ChangeCell(transition.Grid, 1, transition.Grid.Cells[1] with { Symbol = C }) },
            "lost survivor" => transition with { Grid = ChangeCell(transition.Grid, 1, new(transition.NextInstanceId, C)), NextInstanceId = transition.NextInstanceId + 1 },
            "missing movement" => transition with { Movements = transition.Movements.RemoveAt(0) },
            "wrong movement" => transition with { Movements = transition.Movements.SetItem(0, transition.Movements[0] with { To = new(0, 0) }) },
            "duplicate movement" => transition with { Movements = transition.Movements.SetItem(1, transition.Movements[0]) },
            "missing arrival" => transition with { Arrivals = transition.Arrivals.RemoveAt(0) },
            "wrong arrival" => transition with { Arrivals = transition.Arrivals.SetItem(0, transition.Arrivals[0] with { Position = new(0, 1) }) },
            "duplicate arrival" => transition with { Arrivals = transition.Arrivals.SetItem(1, transition.Arrivals[0]) },
            "skipped instance id" => transition with { NextInstanceId = transition.NextInstanceId + 1 },
            "wrong removals" => transition with { Removed = [] },
            _ => transition with { Arrivals = default }
        });
        var game = Compose(original, policy, original.WinLimit);
        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("invalid", 100), new Zeros(), 1));
        Assert.Equal(1, policy.Calls);
    }

    [Fact]
    public void CompleteEvidence_ShouldAllowDifferentRecordOrdering()
    {
        var original = Fixture();
        var policy = new ChangedTransition(original.Cascades, (_, transition) => transition with
        {
            Movements = transition.Movements.Reverse().ToImmutableArray(),
            Arrivals = transition.Arrivals.Reverse().ToImmutableArray()
        });
        var game = Compose(original, policy, original.WinLimit);
        var result = new SlotEngine().Evaluate(game, new("valid", 100), new Tape(0, 0, 0, 0, 1, 0));
        Assert.True(result.IsComplete);
        Assert.Equal(500, result.TotalPayoutUnits);
    }

    private static SymbolBoard ChangeCell(SymbolBoard board, int index, SymbolInstance cell) =>
        new(board.ReelCount, board.RowCount, board.Cells.SetItem(index, cell));

    private sealed class ChangedTransition(ICascadePolicy inner,
        Func<SymbolBoard, CascadeTransition, CascadeTransition> change) : ICascadePolicy
    {
        public int Calls { get; private set; }
        public string ConfigurationKey => "test-transition-v1";
        public ImmutableHashSet<SymbolId> RefillSymbols => inner.RefillSymbols;
        public void Validate(GameDefinition game) => inner.Validate(game);
        public ImmutableArray<GridPosition> SelectRemovals(GameDefinition game, SymbolBoard grid, ImmutableArray<WinAward> wins) =>
            inner.SelectRemovals(game, grid, wins);
        public CascadeTransition Apply(GameDefinition game, SpinMode mode, SymbolBoard grid,
            ImmutableArray<GridPosition> removed, long nextInstanceId, DrawSequence draws)
        {
            Calls++;
            return change(grid, inner.Apply(game, mode, grid, removed, nextInstanceId, draws));
        }
    }
}
