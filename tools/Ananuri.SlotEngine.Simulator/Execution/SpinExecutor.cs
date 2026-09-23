using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Execution;

internal static class SpinExecutor
{
    internal static SpinEvaluation EvaluateComplete(ISlotEngine engine, GameDefinition game,
        SpinRequest request, IRandomDrawSource source, RoundExecutionBudget? budget = null)
    {
        budget ??= new RoundExecutionBudget();
        budget.BeginSpin();
        source = new CancellableDrawSource(source, budget.CancellationToken);
        var result = engine.Evaluate(game, request, source, budget.NextGridBatch());
        budget.ObserveGrids(result.Steps.Length);
        if (result.IsComplete) return result;
        var steps = ImmutableArray.CreateBuilder<CascadeStep>();
        steps.AddRange(result.Steps);
        while (result.Continuation is not null)
        {
            result = engine.Resume(game, result.Continuation, source, budget.NextGridBatch());
            budget.ObserveGrids(result.Steps.Length);
            steps.AddRange(result.Steps);
        }
        return result with { Steps = steps.ToImmutable() };
    }

    private sealed class CancellableDrawSource(IRandomDrawSource source, CancellationToken cancellationToken) : IRandomDrawSource
    {
        public int Next(DrawRequest request)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return source.Next(request);
        }
    }
}
