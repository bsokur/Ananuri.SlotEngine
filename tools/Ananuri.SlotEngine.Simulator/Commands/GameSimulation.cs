using System.Diagnostics;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Simulator.Execution;
using Ananuri.SlotEngine.Simulator.Randomness;
using Ananuri.SlotEngine.Simulator.Replay;
using Ananuri.SlotEngine.Simulator.Reporting;
using Ananuri.SlotEngine.Simulator.Statistics;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Commands;

internal static class GameSimulation
{
    public static void Run(string[] args, CancellationToken cancellationToken = default)
    {
        var options = GameSimulationOptions.Parse(args);
        bool demo = options.Demo;
        int count = options.Count, seed = options.Seed;
        var game = GamePackageFiles.Load(options.PackagePath, "CascadingGame.json");
        long stake = game.Bets.AllowedTotalStakeUnits[0];
        var engine = new SlotEngine();
        IRandomDrawSource source = new OfflineRandomSource(seed, forceFirstStops: demo);
        RecordingDrawSource? recording = demo ? new RecordingDrawSource(source) : null;
        source = recording ?? source;
        var replayResults = new List<(SpinRequest Request, SpinEvaluation Result)>();
        var statistics = new GameStatistics();
        var timer = Stopwatch.StartNew();

        for (int round = 0; round < count; round++)
        {
            var budget = new RoundExecutionBudget(options.MaxGridsPerRound, options.MaxSpinsPerRound, cancellationToken);
            var request = new SpinRequest($"round-{round}/paid", stake);
            var result = SpinExecutor.EvaluateComplete(engine, game, request, source, budget);
            if (demo) replayResults.Add((request, result));
            statistics.ObservePaidSpin(result);
            int bonusLength = 0;
            while (result.NextBonus is not null)
            {
                request = new SpinRequest($"round-{round}/free-{bonusLength}", stake, result.NextBonus);
                result = SpinExecutor.EvaluateComplete(engine, game, request, source, budget);
                if (demo) replayResults.Add((request, result));
                statistics.ObserveFreeSpin(result);
                bonusLength++;
            }
            statistics.ObserveRound(result, bonusLength, stake);
        }
        timer.Stop();
        cancellationToken.ThrowIfCancellationRequested();
        if (recording is not null) ReplayVerifier.Verify(engine, game, recording, replayResults, options, cancellationToken);
        foreach (var (_, result) in replayResults) GameReport.PrintSpin(result);
        GameReport.PrintSummary(game, options, statistics, stake, timer.Elapsed);
    }
}
