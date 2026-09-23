using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Removes awarded cells, moves survivors downward, and refills empty cells at the top of each reel.</summary>
public sealed class FallingSymbolsCascade : ICascadePolicy
{
    public FallingSymbolsCascade(IRefillPolicy refill) => Refill = refill ?? throw new ArgumentNullException(nameof(refill));
    public IRefillPolicy Refill { get; }
    public ImmutableHashSet<SymbolId> RefillSymbols => Refill.Symbols;
    public string ConfigurationKey => $"falling-awarded-positions-v1[{Refill.ConfigurationKey}]";
    public void Validate(GameDefinition game) => Refill.Validate(game);
    public ImmutableArray<GridPosition> SelectRemovals(GameDefinition game, SymbolBoard grid, ImmutableArray<WinAward> wins) =>
        wins.SelectMany(w => w.Positions).Distinct().OrderBy(p => p.Reel).ThenBy(p => p.Row).ToImmutableArray();
    public CascadeTransition Apply(GameDefinition game, SpinMode mode, SymbolBoard grid,
        ImmutableArray<GridPosition> removed, long nextInstanceId, DrawSequence draws)
    {
        var unique = removed.Distinct().OrderBy(p => p.Reel).ThenBy(p => p.Row).ToImmutableArray();
        foreach (var p in unique) _ = grid[p.Reel, p.Row];
        var removal = unique.ToHashSet();
        var cells = grid.Cells.ToArray();
        var moves = ImmutableArray.CreateBuilder<SymbolMovement>();
        var arrivals = ImmutableArray.CreateBuilder<SymbolArrival>();
        for (int reel = 0; reel < grid.ReelCount; reel++)
        {
            int destination = grid.RowCount - 1;
            for (int row = grid.RowCount - 1; row >= 0; row--)
            {
                if (removal.Contains(new GridPosition(reel, row))) continue;
                var cell = grid[reel, row];
                cells[reel * grid.RowCount + destination] = cell;
                if (row != destination) moves.Add(new(cell.Id, new(reel, row), new(reel, destination)));
                destination--;
            }
            for (int row = 0; row <= destination; row++)
            {
                var cell = new SymbolInstance(nextInstanceId++, Refill.NextSymbol(game, mode, reel, row, draws));
                cells[reel * grid.RowCount + row] = cell;
                arrivals.Add(new(cell, new(reel, row)));
            }
        }
        return new(new SymbolBoard(grid.ReelCount, grid.RowCount, cells), unique, moves.ToImmutable(), arrivals.ToImmutable(), nextInstanceId);
    }
}
