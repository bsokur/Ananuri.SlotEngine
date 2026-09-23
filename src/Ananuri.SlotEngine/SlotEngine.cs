using Ananuri.SlotEngine.Definitions;
using System.Collections.Immutable;
using System.Reflection;
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
        SpinExecutionValidator.ValidateRequest(game, request);
        ArgumentNullException.ThrowIfNull(draws);
        if (maxGridEvaluations <= 0) throw new ArgumentOutOfRangeException(nameof(maxGridEvaluations));
        var sequence = new DrawSequence(request.EvaluationId, draws);
        var generated = game.GridGenerator.Generate(game, request.Mode, sequence);
        var strategy = Strategy(game, request.Mode);
        long multiplier = request.Bonus is not null && strategy.Persistence == MultiplierPersistence.Bonus
            ? request.Bonus.Multiplier : strategy.Start;
        var state = new SpinContinuation(game.Fingerprint, SlotEngine.EngineVersion, RulesVersion, request,
            generated.Grid, generated.ReelStops, generated.NextInstanceId, 0, sequence.NextOrdinal, multiplier,
            ImmutableHashSet<long>.Empty, ImmutableHashSet<long>.Empty, 0, request.Bonus?.RoundPayoutUnits ?? 0, 0);
        return Resume(game, state, draws, maxGridEvaluations);
    }

    public SpinEvaluation Resume(GameDefinition game, SpinContinuation continuation,
        IRandomDrawSource draws, int maxGridEvaluations = 1_000)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        SpinExecutionValidator.ValidateRequest(game, continuation.Request);
        ArgumentNullException.ThrowIfNull(draws);
        if (maxGridEvaluations <= 0) throw new ArgumentOutOfRangeException(nameof(maxGridEvaluations));
        SpinExecutionValidator.ValidateContinuation(game, continuation);
        var state = continuation;
        var request = state.Request;
        var sequence = new DrawSequence(request.EvaluationId, draws, state.NextDrawOrdinal);
        var strategy = Strategy(game, request.Mode);
        var steps = ImmutableArray.CreateBuilder<CascadeStep>();
        for (int processed = 0; processed < maxGridEvaluations; processed++)
        {
            var evaluated = GridStepEvaluator.Evaluate(game, state, strategy, sequence);
            state = evaluated.State;
            steps.Add(evaluated.Step);
            if (evaluated.IsComplete)
            {
                var feature = game.Features.Complete(game, request, state.PendingFreeSpins,
                    state.Multiplier, state.RoundPayoutUnits, evaluated.WinLimitReached);
                FeatureTransitionValidator.Validate(game, state, feature, evaluated.WinLimitReached);
                return Result(game, state, steps.ToImmutable(), feature,
                    evaluated.WinLimitReached ? CompletionReason.WinLimit : CompletionReason.NoMoreWins, null);
            }
        }
        return Result(game, state, steps.ToImmutable(), new FeatureTransition(null, 0), CompletionReason.WorkBudget, state);
    }

    private static SpinEvaluation Result(GameDefinition game, SpinContinuation state,
        ImmutableArray<CascadeStep> steps, FeatureTransition feature, CompletionReason reason, SpinContinuation? continuation) =>
        new(state.Request.EvaluationId, game.GameId, game.MathVersion, game.Fingerprint,
            SlotEngine.EngineVersion, RulesVersion, state.Request.Mode, state.Request.ChargedStakeUnits,
            state.Request.CalculationStakeUnits, state.Request.Bonus, state.InitialStops, steps,
            state.SpinPayoutUnits, state.RoundPayoutUnits, feature.GrantedFreeSpins, feature.Bonus, reason, continuation);

    private static IMultiplierStrategy Strategy(GameDefinition game, SpinMode mode) =>
        mode == SpinMode.Paid ? game.PaidMultiplier : game.BonusMultiplier;
}
