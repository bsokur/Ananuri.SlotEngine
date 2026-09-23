using Ananuri.SlotEngine.Simulator.Execution;

namespace Ananuri.SlotEngine.Simulator.Commands;

internal sealed record GameSimulationOptions(bool Demo, int Count, int Seed, string? PackagePath,
    int MaxGridsPerRound = RoundExecutionBudget.DefaultMaxGrids, int MaxSpinsPerRound = RoundExecutionBudget.DefaultMaxSpins)
{
    internal const string Usage = "game-demo [package.json] or game-simulate [rounds] [seed] [package.json], with optional --max-grids-per-round N and --max-spins-per-round N";

    internal static GameSimulationOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("game-demo" or "game-simulate"))
            throw new ArgumentException($"Use {Usage}.");
        bool demo = args[0] == "game-demo";
        var arguments = CommandArguments.Parse(args.Skip(1), "--max-grids-per-round", "--max-spins-per-round");
        arguments.RequireMaximumPositionals(demo ? 1 : 3, Usage);
        var positionals = arguments.Positionals;
        int count = demo ? 1 : positionals.Count > 0 ? CommandArguments.PositiveInteger(positionals[0], "rounds") : 10_000;
        int seed = !demo && positionals.Count > 1 ? CommandArguments.Integer(positionals[1], "seed") : 12345;
        string? packagePath = demo ? positionals.FirstOrDefault() : positionals.ElementAtOrDefault(2);
        return new(demo, count, seed, packagePath,
            arguments.Option("--max-grids-per-round", RoundExecutionBudget.DefaultMaxGrids),
            arguments.Option("--max-spins-per-round", RoundExecutionBudget.DefaultMaxSpins));
    }
}
