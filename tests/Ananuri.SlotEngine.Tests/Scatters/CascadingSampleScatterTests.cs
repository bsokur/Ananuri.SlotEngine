using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Scatters;

public sealed class CascadingSampleScatterTests
{
    [Theory]
    [InlineData(2, 100, 0, 0)]
    [InlineData(3, 100, 100, 7)]
    [InlineData(4, 100, 200, 11)]
    [InlineData(5, 100, 500, 13)]
    [InlineData(3, 200, 200, 7)]
    [InlineData(4, 200, 400, 11)]
    [InlineData(5, 200, 1000, 13)]
    [InlineData(3, 500, 500, 7)]
    [InlineData(4, 500, 1000, 11)]
    [InlineData(5, 500, 2500, 13)]
    public void PaidTriggersPayCashWithoutAnyLineWin(int count, long stake, long payout, int spins)
    {
        var game = LoadSample("CascadingGame.json");
        int[] stops = [13, 14, count >= 3 ? 15 : 1, count >= 4 ? 5 : 0, count >= 5 ? 5 : 0];

        var result = new SlotEngine().Evaluate(game, new("scatter-only", stake), new FixedReelStops(game, stops));

        var step = Assert.Single(result.Steps);
        Assert.Empty(step.Awards);
        Assert.Equal(count, step.Scatter.Count);
        Assert.Equal(payout, step.Scatter.BasePayoutUnits);
        Assert.Equal(payout, result.TotalPayoutUnits);
        Assert.Equal(spins, result.GrantedFreeSpins);
        Assert.Equal(spins, result.NextBonus?.RemainingSpins ?? 0);
        Assert.Null(step.Transition);
        Assert.True(result.IsComplete);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FreeSpinRetriggersDoNotPayScatterCash(int count)
    {
        var game = LoadSample("CascadingGame.json");
        int[] stops = [13, 14, 15, count >= 4 ? 5 : 0, count >= 5 ? 5 : 0];
        var result = new SlotEngine().Evaluate(game, new("retrigger", 100, Bonus(game, multiplier: 3)),
            new FixedReelStops(game, stops, SpinMode.Free));

        var step = Assert.Single(result.Steps);
        Assert.Empty(step.Awards);
        Assert.Equal(count, step.Scatter.Count);
        Assert.Equal(0, step.Scatter.BasePayoutUnits);
        Assert.Equal(0, result.TotalPayoutUnits);
        Assert.Equal(3, result.GrantedFreeSpins);
        Assert.True(result.IsComplete);
    }
}
