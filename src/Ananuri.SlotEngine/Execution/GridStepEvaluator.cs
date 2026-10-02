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
        IMultiplierStrategy multiplierStrategy, DrawSequence sequence)
    {
        var request = state.Request;

        // Both award evaluators see the current board before any symbols are removed.
        var scatter = game.Scatters.Evaluate(game, new ScatterContext(
            Mode: request.Mode,
            GridIndex: state.NextGridIndex,
            Grid: state.Grid,
            CalculationStakeUnits: request.CalculationStakeUnits,
            CountedInstanceIds: state.CountedScatterInstanceIds,
            FeatureAlreadyTriggered: state.PendingFreeSpins > 0));
        ScatterResultValidator.Validate(game, scatter, state.Grid, state.CountedScatterInstanceIds, request.CalculationStakeUnits);
        var wins = game.Wins.Evaluate(game, state.Grid, request.CalculationStakeUnits);
        WinAwardValidator.Validate(game, wins, state.Grid, request.CalculationStakeUnits);

        var multiplier = multiplierStrategy.Apply(new MultiplierState(state.Multiplier), new MultiplierContext(
            Mode: request.Mode,
            GridIndex: state.NextGridIndex,
            Grid: state.Grid,
            Wins: wins,
            CollectedInstanceIds: state.CollectedInstanceIds));
        var collectedInstanceIds = MultiplierResultValidator.Validate(multiplierStrategy, multiplier, state.Grid, state.CollectedInstanceIds);
        var payout = GridPayoutCalculator.Calculate(game, state, wins, multiplier.AppliedMultiplier, scatter.BasePayoutUnits);
        long pendingFreeSpins = checked(state.PendingFreeSpins + scatter.RequestedFreeSpins);

        var transition = ApplyCascadeIfNeeded(game, state, wins, sequence, payout.WinLimitReached);
        bool isComplete = transition is null;
        // A capped grid still pays with its applied multiplier, but does not advance retained multiplier state.
        long nextMultiplier = payout.WinLimitReached ? state.Multiplier : multiplier.NextState.Value;
        var step = new CascadeStep(
            GridIndex: state.NextGridIndex,
            Grid: state.Grid,
            Awards: payout.Awards,
            Scatter: scatter,
            ScatterAppliedMultiplier: payout.ScatterAppliedMultiplier,
            MultiplierBefore: state.Multiplier,
            AppliedMultiplier: multiplier.AppliedMultiplier,
            MultiplierAfter: nextMultiplier,
            Collections: multiplier.Collections,
            RawPayoutUnits: payout.RawPayoutUnits,
            PayablePayoutUnits: payout.PayablePayoutUnits,
            CapAdjustmentUnits: payout.CapAdjustmentUnits,
            Transition: transition);

        // Keep the evaluated board in the step; a continuing spin resumes from the refilled board.
        var nextState = state with
        {
            Grid = transition?.Grid ?? state.Grid,
            NextInstanceId = transition?.NextInstanceId ?? state.NextInstanceId,
            NextGridIndex = checked(state.NextGridIndex + 1),
            NextDrawOrdinal = sequence.NextOrdinal,
            Multiplier = nextMultiplier,
            CollectedInstanceIds = collectedInstanceIds,
            CountedScatterInstanceIds = state.CountedScatterInstanceIds.Union(scatter.CountedInstanceIds),
            SpinPayoutUnits = payout.SpinPayoutUnits,
            RoundPayoutUnits = payout.RoundPayoutUnits,
            PendingFreeSpins = pendingFreeSpins
        };
        return new(State: nextState, Step: step, IsComplete: isComplete, WinLimitReached: payout.WinLimitReached);
    }

    private static CascadeTransition? ApplyCascadeIfNeeded(GameDefinition game, SpinContinuation state,
        ImmutableArray<WinAward> wins, DrawSequence sequence, bool winLimitReached)
    {
        var removedPositions = game.Cascades.SelectRemovals(game, state.Grid, wins).Distinct()
            .OrderBy(position => position.Reel).ThenBy(position => position.Row).ToImmutableArray();
        foreach (var position in removedPositions) _ = state.Grid[position.Reel, position.Row];
        if (winLimitReached || removedPositions.IsEmpty) return null;

        var transition = game.Cascades.Apply(game, state.Request.Mode, state.Grid, removedPositions, state.NextInstanceId, sequence);
        CascadeTransitionValidator.Validate(game, state.Grid, removedPositions, state.NextInstanceId, transition);
        return transition;
    }
}
