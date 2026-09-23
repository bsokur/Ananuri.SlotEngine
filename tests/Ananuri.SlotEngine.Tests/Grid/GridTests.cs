using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Grid;

public sealed class GridTests
{
    [Fact]
    public void Gravity_ShouldPreserveSurvivorOrderAndIds_AndFillInStableOrder()
    {
        var game = WildGame(rows: 4);
        var cells = Enumerable.Range(0, 20).Select(i => new SymbolInstance(i, A)).ToArray();
        var board = new SymbolBoard(5, 4, cells);
        var transition = game.Cascades.Apply(game, SpinMode.Paid, board, [new(0, 1), new(0, 3), new(0, 1)], 20,
            new DrawSequence("id", new Zeros()));
        Assert.Equal(new long[] { 20, 21, 0, 2 }, Enumerable.Range(0, 4).Select(row => transition.Grid[0, row].Id));
        Assert.Equal(2, transition.Removed.Length);
        Assert.Equal(new[] { new GridPosition(0, 0), new GridPosition(0, 1) }, transition.Arrivals.Select(a => a.Position));
        Assert.Equal(22, transition.NextInstanceId);
        Assert.Equal(board[1, 0], transition.Grid[1, 0]);
    }

    [Fact]
    public void WeightedSelection_ShouldMatchExactIntervals()
    {
        var table = new WeightedSymbolTable([new(A, 2), new(B, 1), new(C, 3)]);
        Assert.Equal(new[] { A, A, B, C, C, C }, Enumerable.Range(0, 6).Select(table.Select));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Select(6));
        Assert.Throws<ArgumentException>(() => new WeightedSymbolTable([new(A, int.MaxValue), new(B, 1)]));
    }

    [Fact]
    public void Board_ShouldCaptureCells_AndValidateCoordinatesAndInstanceIds()
    {
        var cells = new[] { new SymbolInstance(0, A), new SymbolInstance(1, B) };
        var board = new SymbolBoard(1, 2, cells);
        cells[0] = new(0, C);
        Assert.Equal(A, board[0, 0].Symbol);
        Assert.Throws<ArgumentOutOfRangeException>(() => board[0, 2]);
        Assert.Throws<ArgumentException>(() => new SymbolBoard(1, 2, new[] { new SymbolInstance(0, A), new SymbolInstance(0, B) }));
    }
}
