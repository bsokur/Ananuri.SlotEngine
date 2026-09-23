using Ananuri.SlotEngine.Samples;

namespace Ananuri.SlotEngine.Simulator.Commands;

internal static class SimulatorApplication
{
    internal static void Run(string[] args, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (args.Length > 0 && args[0] == "tutorial")
        {
            var arguments = CommandArguments.Parse(args.Skip(1));
            arguments.RequireMaximumPositionals(1, "tutorial [package.json]");
            TutorialExample.Run(GamePackageFiles.PathFor(arguments.Positionals.FirstOrDefault(), "FirstGame.json"));
        }
        else if (args.Length > 0 && args[0] is "game-demo" or "game-simulate")
            GameSimulation.Run(args, cancellationToken);
        else
            FixedPaylineSimulation.Run(args, cancellationToken);
    }
}
