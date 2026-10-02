using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Execution;

internal static class WinAwardValidator
{
    internal static void Validate(GameDefinition game, ImmutableArray<WinAward> wins, SymbolBoard grid, long calculationStake)
    {
        if (wins.IsDefault) throw new InvalidOperationException("Win evaluator returned uninitialized awards.");
        BigInteger total = 0;
        foreach (var win in wins)
        {
            if (win is null || win.BasePayoutUnits <= 0 || win.Positions.IsDefaultOrEmpty
                || !game.PayingSymbols.Contains(win.Symbol)
                || win.Positions.Distinct().Count() != win.Positions.Length)
                throw new InvalidOperationException("An ordinary award needs a positive amount and unique contributing positions.");
            foreach (var p in win.Positions) _ = grid[p.Reel, p.Row];
            if (win.WildPositions.IsDefault || win.WildPositions.Any(p => !win.Positions.Contains(p)))
                throw new InvalidOperationException("Wild positions must belong to the selected award.");
            if (win.WildPositions.Any(p => !game.Wins.SubstitutionSymbols.Contains(grid[p.Reel, p.Row].Symbol)
                || !game.Wins.SubstitutionSymbols.Contains(win.Symbol)))
                throw new InvalidOperationException("Win evaluator used an undeclared substitution symbol.");
            total += win.BasePayoutUnits;
        }
        if (total > game.Wins.MaximumBasePayout(game, calculationStake))
            throw new InvalidOperationException("Win evaluator exceeded its declared base payout bound.");
    }
}
