using Ananuri.SlotEngine.Simulator.Commands;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Simulator;

public sealed class CommandArgumentsTests
{
    [Fact]
    public void PreservesGameCommandDefaultsAndSupportsBudgetsWithPositionalArguments()
    {
        Assert.Equal(new GameSimulationOptions(false, 10_000, 12345, null), GameSimulationOptions.Parse(["game-simulate"]));
        Assert.Equal(new GameSimulationOptions(true, 1, 12345, "game.json", 25, 3),
            GameSimulationOptions.Parse(["game-demo", "--max-grids-per-round", "25", "game.json", "--max-spins-per-round", "3"]));
        Assert.Equal(new GameSimulationOptions(false, 7, -5, "game.json", 40, 8),
            GameSimulationOptions.Parse(["game-simulate", "7", "-5", "game.json", "--max-grids-per-round", "40", "--max-spins-per-round", "8"]));
    }

    [Theory]
    [InlineData("game-demo game.json extra")]
    [InlineData("game-simulate 5 2 game.json extra")]
    [InlineData("game-simulate 0")]
    [InlineData("game-simulate invalid")]
    [InlineData("game-simulate 4 invalid")]
    [InlineData("game-demo --max-grids-per-round")]
    [InlineData("game-demo --max-grids-per-round 0")]
    [InlineData("game-demo --max-spins-per-round -1")]
    [InlineData("game-demo --max-spins-per-round 2 --max-spins-per-round 3")]
    [InlineData("game-demo --unknown 1")]
    public void RejectsInvalidGameCommandArguments(string command)
    {
        Assert.ThrowsAny<ArgumentException>(() => GameSimulationOptions.Parse(command.Split(' ')));
    }

    [Theory]
    [InlineData("demo extra")]
    [InlineData("enumerate game.json extra")]
    [InlineData("simulate 10 3 extra")]
    [InlineData("tutorial game.json extra")]
    [InlineData("unknown")]
    [InlineData("enumerate --max-combinations 0")]
    [InlineData("enumerate --max-combinations")]
    [InlineData("simulate --unknown 1")]
    public void RejectsExtraOrInvalidArgumentsBeforeLoadingFiles(string command)
    {
        Assert.ThrowsAny<ArgumentException>(() => SimulatorApplication.Run(command.Split(' '), TestContext.Current.CancellationToken));
    }
}
