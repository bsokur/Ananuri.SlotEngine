using System.Diagnostics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Simulator.Execution;
using Ananuri.SlotEngine.Simulator.Reporting;
using Ananuri.SlotEngine.Simulator.Statistics;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Commands;

internal static class FixedPaylineSimulation
{
    internal const int DefaultMaxCombinations = 1_000_000;

    internal static void Run(string[] args, CancellationToken cancellationToken = default)
    {
        string command = args.FirstOrDefault() ?? "demo";
        var arguments = CommandArguments.Parse(args.Skip(1), command == "enumerate" ? ["--max-combinations"] : []);
        if (command == "demo")
        {
            arguments.RequireMaximumPositionals(0, "demo");
            PrintDemo(cancellationToken);
            return;
        }
        if (command is not ("enumerate" or "simulate"))
            throw new ArgumentException("Use tutorial [package.json], demo, enumerate [package.json] [--max-combinations N], simulate [count] [seed], or " + GameSimulationOptions.Usage + ".");

        arguments.RequireMaximumPositionals(command == "enumerate" ? 1 : 2,
            command == "enumerate" ? "enumerate [package.json] [--max-combinations N]" : "simulate [count] [seed]");
        var game = GamePackageFiles.Load(command == "enumerate" ? arguments.Positionals.FirstOrDefault() : null, "PaylineGame.json");
        var timer = Stopwatch.StartNew();
        PaylineStatistics statistics;
        string heading;
        if (command == "enumerate")
        {
            statistics = Enumerate(game, arguments.Option("--max-combinations", DefaultMaxCombinations), cancellationToken);
            heading = "Exact enumeration of all initial reel stops; independent uniform reel stops.";
        }
        else
        {
            int count = arguments.Positionals.Count > 0 ? CommandArguments.PositiveInteger(arguments.Positionals[0], "count") : 100_000;
            int seed = arguments.Positionals.Count > 1 ? CommandArguments.Integer(arguments.Positionals[1], "seed") : 12345;
            statistics = Simulate(game, count, seed, cancellationToken);
            heading = $"Offline sample; seed {seed}; runtime {Environment.Version}. No certification claim.";
        }
        timer.Stop();
        cancellationToken.ThrowIfCancellationRequested();
        PaylineReport.PrintSummary(game, statistics, heading, timer.Elapsed);
    }

    internal static PaylineStatistics Enumerate(GameDefinition game, int maxCombinations = DefaultMaxCombinations,
        CancellationToken cancellationToken = default)
    {
        ValidateSingleBoardGame(game);
        if (maxCombinations <= 0) throw new ArgumentOutOfRangeException(nameof(maxCombinations));
        long combinations = 1;
        foreach (var reel in game.Reels)
        {
            if (combinations > maxCombinations / reel.Length)
                throw new ArgumentException($"Game has more than {maxCombinations} initial-stop combinations. Increase --max-combinations for a larger exact enumeration.", nameof(game));
            combinations *= reel.Length;
        }
        var statistics = new PaylineStatistics();
        var engine = new SlotEngine();
        var stops = new int[game.ReelCount];
        for (long observation = 0; observation < combinations; observation++)
        {
            Observe(engine, game, stops, statistics, cancellationToken);
            for (int reel = stops.Length - 1; reel >= 0; reel--)
            {
                if (++stops[reel] < game.Reels[reel].Length) break;
                stops[reel] = 0;
            }
        }
        return statistics;
    }

    private static PaylineStatistics Simulate(GameDefinition game, int count, int seed, CancellationToken cancellationToken)
    {
        ValidateSingleBoardGame(game);
        var statistics = new PaylineStatistics();
        var engine = new SlotEngine();
        // OFFLINE ONLY. This is not the production RNG.
        var random = new Random(seed);
        var stops = new int[game.ReelCount];
        for (int observation = 0; observation < count; observation++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int reel = 0; reel < game.ReelCount; reel++) stops[reel] = random.Next(game.Reels[reel].Length);
            Observe(engine, game, stops, statistics, cancellationToken);
        }
        return statistics;
    }

    private static void ValidateSingleBoardGame(GameDefinition game)
    {
        if (game.Cascades is not NoCascades || game.Features.SupportsFreeSpins || game.GridGenerator is not ReelStripGridGenerator)
            throw new ArgumentException("Exact initial-stop enumeration requires reel-strip grids, no cascades, and no free-spin feature. Use game-simulate for complete featured rounds.", nameof(game));
    }

    private static void Observe(ISlotEngine engine, GameDefinition game, int[] stops,
        PaylineStatistics statistics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long stake = game.Bets.AllowedTotalStakeUnits[0];
        var request = new SpinRequest($"observation-{statistics.Observations}/paid", stake);
        var result = engine.Evaluate(game, request, new FixedReelStops(game, stops), 1);
        if (!result.IsComplete || result.Steps.Length != 1 || result.NextBonus is not null)
            throw new InvalidOperationException("Initial-stop observation produced an unfinished or featured round.");
        statistics.Observe(result);
    }

    private static void PrintDemo(CancellationToken cancellationToken)
    {
        var game = GamePackageFiles.Load(null, "FiveReelGame.json");
        var request = new SpinRequest("demo/paid", game.Bets.AllowedTotalStakeUnits[0]);
        var timer = Stopwatch.StartNew();
        var result = SpinExecutor.EvaluateComplete(new SlotEngine(), game, request,
            new FixedReelStops(game, new int[game.ReelCount]), new RoundExecutionBudget(cancellationToken: cancellationToken));
        timer.Stop();
        PaylineReport.PrintDemo(result, timer.Elapsed);
    }
}
