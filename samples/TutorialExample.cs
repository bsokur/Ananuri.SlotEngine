using System.Collections.Immutable;
using System.Text.Json;
using Ananuri.SlotEngine;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Samples;

public static class TutorialExample
{
    public static void Run(string packagePath)
    {
        var game = GamePackageLoader.Load(File.ReadAllText(packagePath));
        EvaluateRound(game, Console.Out);
    }

    public static IReadOnlyList<SpinEvaluation> EvaluateRound(GameDefinition game, TextWriter output)
    {
        ISlotEngine engine = new SlotEngine();
        var recording = new RecordingDrawSource(new FirstChoiceDrawSource());
        var evaluations = new List<SpinEvaluation>();
        long stake = game.Bets.AllowedTotalStakeUnits[0];
        BonusState? bonus = null;
        int spin = 0;
        do
        {
            var request = new SpinRequest($"tutorial/{spin}", stake, bonus);
            var result = Complete(engine, game, request, recording);
            evaluations.Add(result);
            output.WriteLine($"{result.EvaluationId}: mode={result.Mode}; charged={result.ChargedStakeUnits}; "
                + $"spin payout={result.TotalPayoutUnits}; round payout={result.RoundPayoutUnits}; "
                + $"remaining free spins={result.NextBonus?.RemainingSpins ?? 0}");

            // Demonstrate serialization between free spins. A real host stores this durably.
            bonus = result.NextBonus is null ? null
                : SpinStateSerializer.DeserializeBonus(SpinStateSerializer.Serialize(result.NextBonus));
            spin++;
        } while (bonus is not null);

        var replay = new ReplayDrawSource(recording.Snapshot());
        foreach (var expected in evaluations)
        {
            var request = new SpinRequest(expected.EvaluationId, expected.CalculationStakeUnits, expected.StartingBonus);
            var actual = Complete(engine, game, request, replay);
            if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(actual))
                throw new InvalidOperationException("Tutorial replay differed from the original result.");
        }
        output.WriteLine($"Replay verified: {evaluations.Count} evaluations; {recording.Snapshot().Length} draws.");
        return evaluations;
    }

    private static SpinEvaluation Complete(ISlotEngine engine, GameDefinition game,
        SpinRequest request, IRandomDrawSource draws)
    {
        // One grid per call deliberately exercises checkpoint/resume on this tiny game.
        var result = engine.Evaluate(game, request, draws, maxGridEvaluations: 1);
        var steps = ImmutableArray.CreateBuilder<CascadeStep>();
        steps.AddRange(result.Steps);
        while (result.Continuation is not null)
        {
            var json = SpinStateSerializer.Serialize(result.Continuation);
            var restored = SpinStateSerializer.DeserializeContinuation(json);
            result = engine.Resume(game, restored, draws, maxGridEvaluations: 1);
            steps.AddRange(result.Steps);
        }
        // Steps are per call; payout totals are already cumulative and must not be added again.
        return result with { Steps = steps.ToImmutable() };
    }

    // Always choose index zero: predictable teaching input, not a random generator.
    private sealed class FirstChoiceDrawSource : IRandomDrawSource
    {
        public int Next(DrawRequest request) => 0;
    }
}
