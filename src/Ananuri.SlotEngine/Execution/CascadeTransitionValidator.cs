using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Execution;

internal static class CascadeTransitionValidator
{
    internal static void Validate(GameDefinition game, SymbolBoard before,
        ImmutableArray<GridPosition> removed, long nextInstanceId, CascadeTransition transition)
    {
        if (transition is null || transition.Removed.IsDefault || transition.Movements.IsDefault
            || transition.Arrivals.IsDefault || !transition.Removed.SequenceEqual(removed)
            || transition.NextInstanceId < nextInstanceId)
            throw new InvalidOperationException("Cascade policy returned incomplete or inconsistent transition evidence.");

        SpinExecutionValidator.ValidateBoard(game, transition.Grid, transition.NextInstanceId);
        var removedIds = removed.Select(p => before[p.Reel, p.Row].Id).ToHashSet();
        var survivors = new Dictionary<long, (SymbolInstance Cell, GridPosition Position)>();
        for (int reel = 0; reel < before.ReelCount; reel++)
            for (int row = 0; row < before.RowCount; row++)
            {
                var cell = before[reel, row];
                if (!removedIds.Contains(cell.Id)) survivors.Add(cell.Id, (cell, new(reel, row)));
            }

        var expectedMoves = new HashSet<SymbolMovement>();
        var expectedArrivals = new HashSet<SymbolArrival>();
        for (int reel = 0; reel < transition.Grid.ReelCount; reel++)
            for (int row = 0; row < transition.Grid.RowCount; row++)
            {
                var cell = transition.Grid[reel, row];
                var position = new GridPosition(reel, row);
                if (removedIds.Contains(cell.Id))
                    throw new InvalidOperationException("Cascade policy retained a removed symbol instance.");
                if (survivors.Remove(cell.Id, out var survivor))
                {
                    if (cell.Symbol != survivor.Cell.Symbol)
                        throw new InvalidOperationException("Cascade policy changed a surviving symbol instance.");
                    if (position != survivor.Position) expectedMoves.Add(new(cell.Id, survivor.Position, position));
                }
                else
                {
                    if (cell.Id < nextInstanceId)
                        throw new InvalidOperationException("Cascade policy reused an earlier symbol instance ID.");
                    if (!game.Cascades.RefillSymbols.Contains(cell.Symbol))
                        throw new InvalidOperationException("Cascade policy returned an undeclared refill symbol.");
                    expectedArrivals.Add(new(cell, position));
                }
            }

        if (survivors.Count != 0 || expectedArrivals.Count != removed.Length
            || transition.NextInstanceId - nextInstanceId != expectedArrivals.Count)
            throw new InvalidOperationException("Cascade policy lost survivors or returned an invalid instance sequence.");
        // Board IDs are unique and bounded; the count/range checks above imply contiguous fresh IDs.
        if (transition.Movements.Length != expectedMoves.Count || !expectedMoves.SetEquals(transition.Movements)
            || transition.Movements.Distinct().Count() != transition.Movements.Length
            || transition.Arrivals.Length != expectedArrivals.Count || !expectedArrivals.SetEquals(transition.Arrivals)
            || transition.Arrivals.Distinct().Count() != transition.Arrivals.Length)
            throw new InvalidOperationException("Cascade movement or arrival evidence does not match the boards.");
    }
}
