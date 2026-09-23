using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Execution;

internal static class SpinExecutionValidator
{
    internal static void ValidateRequest(GameDefinition game, SpinRequest request)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EvaluationId);
        if (!game.Bets.Allows(request.CalculationStakeUnits)) throw new ArgumentException("Unsupported calculation stake.");
        if (request.Bonus is not null)
        {
            if (!game.Features.SupportsFreeSpins) throw new ArgumentException("This game has no free-spin feature.");
            game.Features.ValidateState(game, request.Bonus);
            if (request.CalculationStakeUnits != request.Bonus.CalculationStakeUnits)
                throw new ArgumentException("Free-spin calculation stake must match the triggering stake.");
        }
    }

    internal static void ValidateContinuation(GameDefinition game, SpinContinuation state)
    {
        var strategy = state.Request.Mode == SpinMode.Paid ? game.PaidMultiplier : game.BonusMultiplier;
        long originalPayout = state.Request.Bonus?.RoundPayoutUnits ?? 0;
        if (state.GameFingerprint != game.Fingerprint || state.EngineVersion != SlotEngine.EngineVersion
            || state.RulesVersion != SlotEngine.RulesVersion || state.NextGridIndex < 0 || state.NextDrawOrdinal < 0
            || state.Multiplier < strategy.Start || state.Multiplier > strategy.Maximum
            || state.SpinPayoutUnits < 0 || state.RoundPayoutUnits < originalPayout
            || (game.WinLimit.MaximumPayout(state.Request.CalculationStakeUnits) is long limit && state.RoundPayoutUnits >= limit)
            || state.RoundPayoutUnits - originalPayout != state.SpinPayoutUnits
            || state.PendingFreeSpins < 0 || state.CollectedInstanceIds is null || state.CountedScatterInstanceIds is null
            || state.CollectedInstanceIds.Any(id => id < 0 || id >= state.NextInstanceId)
            || state.CountedScatterInstanceIds.Any(id => id < 0 || id >= state.NextInstanceId)
            || state.InitialStops.IsDefault || state.InitialStops.Length != game.ReelCount)
            throw new ArgumentException("Invalid continuation or package/engine mismatch.");
        var reels = state.Request.Mode == SpinMode.Paid ? game.Reels : game.FreeSpinReels;
        for (int reel = 0; reel < reels.Length; reel++)
            if (state.InitialStops[reel] < 0 || state.InitialStops[reel] >= reels[reel].Length)
                throw new ArgumentException("Invalid continuation reel stop.");
        ValidateBoard(game, state.Grid, state.NextInstanceId);
    }

    internal static void ValidateBoard(GameDefinition game, SymbolBoard grid, long nextInstanceId)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.ReelCount != game.ReelCount || grid.RowCount != game.VisibleRows
            || grid.Cells.Any(c => c.Id >= nextInstanceId || !game.Symbols.Contains(c.Symbol)))
            throw new ArgumentException("Invalid grid dimensions, symbols, or instance sequence.");
    }

    internal static void ValidateAwards(GameDefinition game, ImmutableArray<WinAward> wins, SymbolBoard grid, long calculationStake)
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

    internal static void ValidateScatter(GameDefinition game, ScatterResult result, SymbolBoard grid,
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
