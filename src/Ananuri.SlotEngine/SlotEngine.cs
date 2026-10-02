using System.Collections.Immutable;
using System.Reflection;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Execution;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine;

/// <summary>Stateless coordinator. Runtime state and random allocations must be trusted and persisted by the host.</summary>
public sealed class SlotEngine : ISlotEngine
{
    /// <summary>Identifies the execution rules required by persisted state and replay records.</summary>
    public const string RulesVersion = "slot-engine-v1";
    /// <summary>Identifies this assembly build for persisted-state and replay compatibility checks.</summary>
    public static string EngineVersion { get; } = typeof(SlotEngine).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? throw new InvalidOperationException("Engine build version is missing.");

    public SpinEvaluation Evaluate(GameDefinition game, SpinRequest request,
        IRandomDrawSource draws, int maxGridEvaluations = 1_000)
    {
        SpinRequestValidator.Validate(game, request);
        ArgumentNullException.ThrowIfNull(draws);
        if (maxGridEvaluations <= 0) throw new ArgumentOutOfRangeException(nameof(maxGridEvaluations));

        var sequence = new DrawSequence(request.EvaluationId, draws);
        var generated = game.GridGenerator.Generate(game, request.Mode, sequence);
        var multiplierStrategy = SelectMultiplierStrategy(game, request.Mode);
        long startingMultiplier = request.Bonus is not null && multiplierStrategy.Persistence == MultiplierPersistence.Bonus
            ? request.Bonus.Multiplier : multiplierStrategy.Start;
        var state = new SpinContinuation(
            GameFingerprint: game.Fingerprint,
            EngineVersion: EngineVersion,
            RulesVersion: RulesVersion,
            Request: request,
            Grid: generated.Grid,
            InitialStops: generated.ReelStops,
            NextInstanceId: generated.NextInstanceId,
            NextGridIndex: 0,
            NextDrawOrdinal: sequence.NextOrdinal,
            Multiplier: startingMultiplier,
            CollectedInstanceIds: ImmutableHashSet<long>.Empty,
            CountedScatterInstanceIds: ImmutableHashSet<long>.Empty,
            SpinPayoutUnits: 0,
            RoundPayoutUnits: request.Bonus?.RoundPayoutUnits ?? 0,
            PendingFreeSpins: 0);
        return Resume(game, state, draws, maxGridEvaluations);
    }

    public SpinEvaluation Resume(GameDefinition game, SpinContinuation continuation,
        IRandomDrawSource draws, int maxGridEvaluations = 1_000)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        SpinRequestValidator.Validate(game, continuation.Request);
        ArgumentNullException.ThrowIfNull(draws);
        if (maxGridEvaluations <= 0) throw new ArgumentOutOfRangeException(nameof(maxGridEvaluations));
        SpinContinuationValidator.Validate(game, continuation);

        var state = continuation;
        var request = state.Request;
        var sequence = new DrawSequence(request.EvaluationId, draws, state.NextDrawOrdinal);
        var multiplierStrategy = SelectMultiplierStrategy(game, request.Mode);
        var steps = ImmutableArray.CreateBuilder<CascadeStep>();
        for (int evaluatedGridCount = 0; evaluatedGridCount < maxGridEvaluations; evaluatedGridCount++)
        {
            var gridResult = GridStepEvaluator.Evaluate(game, state, multiplierStrategy, sequence);
            state = gridResult.State;
            steps.Add(gridResult.Step);
            if (gridResult.IsComplete)
            {
                // Scatter requests become actual free-spin grants only after the spin's final grid.
                var featureTransition = game.Features.Complete(game, request, state.PendingFreeSpins,
                    state.Multiplier, state.RoundPayoutUnits, gridResult.WinLimitReached);
                FeatureTransitionValidator.Validate(game, state, featureTransition, gridResult.WinLimitReached);
                return CreateSpinEvaluation(game, state, steps.ToImmutable(), featureTransition,
                    gridResult.WinLimitReached ? CompletionReason.WinLimit : CompletionReason.NoMoreWins,
                    continuation: null);
            }
        }

        // A checkpoint resumes this same spin; it does not start the next free spin.
        return CreateSpinEvaluation(game, state, steps.ToImmutable(),
            new FeatureTransition(Bonus: null, GrantedFreeSpins: 0), CompletionReason.WorkBudget, continuation: state);
    }

    private static SpinEvaluation CreateSpinEvaluation(GameDefinition game, SpinContinuation state,
        ImmutableArray<CascadeStep> steps, FeatureTransition featureTransition, CompletionReason reason, SpinContinuation? continuation) =>
        new(
            EvaluationId: state.Request.EvaluationId,
            GameId: game.GameId,
            MathVersion: game.MathVersion,
            GameFingerprint: game.Fingerprint,
            EngineVersion: EngineVersion,
            RulesVersion: RulesVersion,
            Mode: state.Request.Mode,
            ChargedStakeUnits: state.Request.ChargedStakeUnits,
            CalculationStakeUnits: state.Request.CalculationStakeUnits,
            StartingBonus: state.Request.Bonus,
            InitialStops: state.InitialStops,
            Steps: steps,
            TotalPayoutUnits: state.SpinPayoutUnits,
            RoundPayoutUnits: state.RoundPayoutUnits,
            GrantedFreeSpins: featureTransition.GrantedFreeSpins,
            NextBonus: featureTransition.Bonus,
            CompletionReason: reason,
            Continuation: continuation);

    private static IMultiplierStrategy SelectMultiplierStrategy(GameDefinition game, SpinMode mode) =>
        mode == SpinMode.Paid ? game.PaidMultiplier : game.BonusMultiplier;
}
