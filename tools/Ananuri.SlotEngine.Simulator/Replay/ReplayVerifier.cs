using System.Text.Json;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Simulator.Commands;
using Ananuri.SlotEngine.Simulator.Execution;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Replay;

internal static class ReplayVerifier
{
    internal static void Verify(ISlotEngine engine, GameDefinition game,
        RecordingDrawSource recording, IReadOnlyList<(SpinRequest Request, SpinEvaluation Result)> replayResults,
        GameSimulationOptions options, CancellationToken cancellationToken = default)
    {
        var replay = new ReplayDrawSource(recording.Snapshot());
        var budget = new RoundExecutionBudget(options.MaxGridsPerRound, options.MaxSpinsPerRound, cancellationToken);
        foreach (var (request, result) in replayResults)
        {
            var repeated = SpinExecutor.EvaluateComplete(engine, game, request, replay, budget);
            if (JsonSerializer.Serialize(result) != JsonSerializer.Serialize(repeated))
                throw new InvalidOperationException("Recorded replay did not reproduce the complete result.");
        }
        Console.WriteLine($"Replay verified: {replayResults.Count} evaluations; {recording.Snapshot().Length} recorded draws.");
    }
}
