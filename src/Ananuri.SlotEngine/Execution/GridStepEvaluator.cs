using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Execution;

internal static class GridStepEvaluator
{
    internal static GridStepResult Evaluate(GameDefinition game, SpinContinuation state,
        IMultiplierStrategy strategy, DrawSequence sequence)
    {
        var request = state.Request;
        var scatter = game.Scatters.Evaluate(game, new ScatterContext(request.Mode, state.NextGridIndex,
            state.Grid, request.CalculationStakeUnits, state.CountedScatterInstanceIds, state.PendingFreeSpins > 0));
        SpinExecutionValidator.ValidateScatter(game, scatter, state.Grid, state.CountedScatterInstanceIds, request.CalculationStakeUnits);
        var wins = game.Wins.Evaluate(game, state.Grid, request.CalculationStakeUnits);
        SpinExecutionValidator.ValidateAwards(game, wins, state.Grid, request.CalculationStakeUnits);
        var multiplier = strategy.Apply(new MultiplierState(state.Multiplier),
            new MultiplierContext(request.Mode, state.NextGridIndex, state.Grid, wins, state.CollectedInstanceIds));
        if (multiplier.AppliedMultiplier < strategy.Start || multiplier.AppliedMultiplier > strategy.Maximum
            || multiplier.NextState.Value < strategy.Start || multiplier.NextState.Value > strategy.Maximum)
            throw new InvalidOperationException("Multiplier strategy returned an out-of-range value.");
        var collected = state.CollectedInstanceIds;
        foreach (var collection in multiplier.Collections)
        {
            var cell = state.Grid[collection.Position.Reel, collection.Position.Row];
            if (collection.InstanceId != cell.Id || collection.Symbol != cell.Symbol || collection.Value <= 0
                || !strategy.CollectionSymbols.Contains(collection.Symbol)
                || collected.Contains(cell.Id)) throw new InvalidOperationException("Invalid or duplicate symbol collection.");
            collected = collected.Add(cell.Id);
        }
        long scatterFactor = game.MultiplyScatterAwards ? multiplier.AppliedMultiplier : 1;
        long raw = checked(scatter.BasePayoutUnits * scatterFactor);
        var awards = ImmutableArray.CreateBuilder<AwardPayout>();
        foreach (var win in wins)
        {
            long amount = checked(win.BasePayoutUnits * multiplier.AppliedMultiplier);
            raw = checked(raw + amount);
            awards.Add(new(win, multiplier.AppliedMultiplier, amount));
        }
        long? maximum = game.WinLimit.MaximumPayout(request.CalculationStakeUnits);
        long? remaining = maximum is long limit ? limit - state.RoundPayoutUnits : null;
        long payable = remaining is long allowance ? Math.Min(raw, allowance) : raw;
        long roundPayout = checked(state.RoundPayoutUnits + payable);
        long spinPayout = checked(state.SpinPayoutUnits + payable);
        bool capped = remaining.HasValue && payable == remaining.Value;
        long pendingSpins = checked(state.PendingFreeSpins + scatter.RequestedFreeSpins);
        var removed = game.Cascades.SelectRemovals(game, state.Grid, wins).Distinct()
            .OrderBy(p => p.Reel).ThenBy(p => p.Row).ToImmutableArray();
        foreach (var position in removed) _ = state.Grid[position.Reel, position.Row];
        bool complete = capped || removed.IsEmpty;
        CascadeTransition? transition = null;
        if (!complete)
        {
            transition = game.Cascades.Apply(game, request.Mode, state.Grid, removed, state.NextInstanceId, sequence);
            CascadeTransitionValidator.Validate(game, state.Grid, removed, state.NextInstanceId, transition);
        }
        long nextMultiplier = capped ? state.Multiplier : multiplier.NextState.Value;
        var step = new CascadeStep(state.NextGridIndex, state.Grid, awards.ToImmutable(), scatter,
            scatterFactor, state.Multiplier, multiplier.AppliedMultiplier, nextMultiplier, multiplier.Collections,
            raw, payable, raw - payable, transition);
        state = state with
        {
            Grid = transition?.Grid ?? state.Grid,
            NextInstanceId = transition?.NextInstanceId ?? state.NextInstanceId,
            NextGridIndex = checked(state.NextGridIndex + 1),
            NextDrawOrdinal = sequence.NextOrdinal,
            Multiplier = nextMultiplier,
            CollectedInstanceIds = collected,
            CountedScatterInstanceIds = state.CountedScatterInstanceIds.Union(scatter.CountedInstanceIds),
            SpinPayoutUnits = spinPayout,
            RoundPayoutUnits = roundPayout,
            PendingFreeSpins = pendingSpins
        };
        return new(state, step, complete, capped);
    }
}
