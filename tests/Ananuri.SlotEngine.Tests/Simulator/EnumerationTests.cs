using System.Text.Json.Nodes;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Simulator.Commands;
using Ananuri.SlotEngine.Tests.Fixtures;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Simulator;

public sealed class EnumerationTests
{
    [Fact]
    public void UsesAllReelLengthsAndConfiguredStakeInsteadOfHardcodedTinyFixtureValues()
    {
        var package = JsonNode.Parse(EngineFixtures.ReadSampleJson("PaylineGame.json"))!.AsObject();
        package["reels"]![0]!.AsArray().Add(1);
        package["allowedStakes"] = new JsonArray(200);
        var game = GamePackageLoader.Load(package.ToJsonString());

        var result = FixedPaylineSimulation.Enumerate(game, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(80, result.Observations);
        Assert.Equal(16000m, result.TotalStake);
        Assert.Equal(16000m, result.TotalPayout);
        Assert.Equal(100m, result.RtpPercent);
        Assert.Equal(14, result.Hits);
        Assert.Equal(new Dictionary<long, long> { [0] = 66, [800] = 2, [1200] = 12 }, result.Distribution);
    }

    [Fact]
    public void RejectsExcessCombinationsBeforeEvaluatingTheGame()
    {
        var game = EngineFixtures.LoadSample("PaylineGame.json");
        var exception = Assert.Throws<ArgumentException>(() => FixedPaylineSimulation.Enumerate(game, 63, TestContext.Current.CancellationToken));
        Assert.Contains("--max-combinations", exception.Message);
        Assert.Equal(64, FixedPaylineSimulation.Enumerate(game, 64, TestContext.Current.CancellationToken).Observations);
    }

    [Theory]
    [InlineData("CascadingGame.json")]
    [InlineData("CollectedSymbolsGame.json")]
    [InlineData("FirstGame.json")]
    public void RejectsFeaturedGamesRatherThanClaimingExactRoundMath(string sample)
    {
        var game = EngineFixtures.LoadSample(sample);
        var exception = Assert.Throws<ArgumentException>(() => FixedPaylineSimulation.Enumerate(game, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("no cascades", exception.Message);
    }

    [Fact]
    public void CanCancelEnumeration()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => FixedPaylineSimulation.Enumerate(
            EngineFixtures.LoadSample("PaylineGame.json"), cancellationToken: cancellation.Token));
    }
}
