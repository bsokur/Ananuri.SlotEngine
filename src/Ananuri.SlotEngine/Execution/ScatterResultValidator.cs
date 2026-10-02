using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Scatters;

namespace Ananuri.SlotEngine.Execution;

internal static class ScatterResultValidator
{
    internal static void Validate(GameDefinition game, ScatterResult result, SymbolBoard grid,
        ImmutableHashSet<long> previouslyCounted, long calculationStake)
    {
        if (result is null || result.BasePayoutUnits < 0 || result.RequestedFreeSpins < 0 || result.Count < 0
            || result.Positions.IsDefault || result.CountedInstanceIds.IsDefault
            || result.Count != result.Positions.Length || result.Positions.Distinct().Count() != result.Positions.Length
            || result.CountedInstanceIds.Distinct().Count() != result.CountedInstanceIds.Length)
            throw new InvalidOperationException("Scatter evaluator returned an invalid award or count.");
        if (result.BasePayoutUnits > (BigInteger)calculationStake * game.Scatters.MaximumAwardMultiplier)
            throw new InvalidOperationException("Scatter evaluator exceeded its declared base payout bound.");
        if (result.RequestedFreeSpins > 0 && (!game.Scatters.CanAwardFreeSpins || !game.Features.SupportsFreeSpins))
            throw new InvalidOperationException("Scatter evaluator requested an undeclared free-spin feature.");
        var currentIds = grid.Cells.Where(c => game.Scatters.Symbols.Contains(c.Symbol)).Select(c => c.Id).ToHashSet();
        if (result.CountedInstanceIds.Any(id => !currentIds.Contains(id) || previouslyCounted.Contains(id)))
            throw new InvalidOperationException("Scatter evaluator reused or invented a symbol instance.");
        foreach (var p in result.Positions)
            if (!result.CountedInstanceIds.Contains(grid[p.Reel, p.Row].Id))
                throw new InvalidOperationException("Scatter positions must correspond to newly counted instances.");
    }
}
