using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Multipliers;

namespace Ananuri.SlotEngine.Execution;

internal static class MultiplierResultValidator
{
    internal static ImmutableHashSet<long> Validate(IMultiplierStrategy strategy, MultiplierResult? result,
        SymbolBoard grid, ImmutableHashSet<long> previouslyCollected)
    {
        if (result is null || result.NextState is null || result.Collections.IsDefault)
            throw new InvalidOperationException("Multiplier strategy returned incomplete state or collection evidence.");
        if (result.AppliedMultiplier < strategy.Start || result.AppliedMultiplier > strategy.Maximum
            || result.NextState.Value < strategy.Start || result.NextState.Value > strategy.Maximum)
            throw new InvalidOperationException("Multiplier strategy returned an out-of-range value.");

        var collected = previouslyCollected;
        foreach (var collection in result.Collections)
        {
            if (collection is null
                || collection.Position.Reel < 0 || collection.Position.Reel >= grid.ReelCount
                || collection.Position.Row < 0 || collection.Position.Row >= grid.RowCount)
                throw new InvalidOperationException("Multiplier strategy returned an invalid collection position.");

            var cell = grid[collection.Position.Reel, collection.Position.Row];
            if (collection.InstanceId != cell.Id || collection.Symbol != cell.Symbol || collection.Value <= 0
                || !strategy.CollectionSymbols.Contains(collection.Symbol) || collected.Contains(cell.Id))
                throw new InvalidOperationException("Invalid or duplicate symbol collection.");
            collected = collected.Add(cell.Id);
        }
        return collected;
    }
}
