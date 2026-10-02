using System.Text.Json;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Samples;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class CustomMultiplierExampleTests
{
    [Theory]
    [InlineData(1, 200, 300)]
    [InlineData(3, 600, 700)]
    public void Example_ShouldBoostOnlyTheInitialGrid(long firstGridMultiplier, long firstGridPayout, long totalPayout)
    {
        var result = CustomMultiplierExample.Run(firstGridMultiplier);

        Assert.True(result.IsComplete);
        Assert.Equal(100, result.ChargedStakeUnits);
        Assert.Equal(totalPayout, result.TotalPayoutUnits);
        Assert.Equal(totalPayout, result.RoundPayoutUnits);
        Assert.Equal(new long[] { firstGridPayout, 100, 0 }, result.Steps.Select(step => step.PayablePayoutUnits));
        Assert.Equal(new long[] { firstGridMultiplier, 1, 1 }, result.Steps.Select(step => step.AppliedMultiplier));
        Assert.Equal(new[] { 1, 2, 3 }, result.Steps.Select(step => step.Grid.Cells[0].Symbol.Value));
        Assert.All(result.Steps, step =>
        {
            Assert.Equal(1, step.MultiplierBefore);
            Assert.Equal(1, step.MultiplierAfter);
            Assert.Empty(step.Collections);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Example_ShouldRejectNonpositiveMultipliers(long firstGridMultiplier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomMultiplierExample.CreateGame(firstGridMultiplier));
    }

    [Fact]
    public void Example_ShouldBindTheMultiplierSettingIntoTheFingerprint()
    {
        var game = CustomMultiplierExample.CreateGame(firstGridMultiplier: 3);

        Assert.Equal("sample-first-grid-multiplier-v1:3", game.PaidMultiplier.ConfigurationKey);
        Assert.Equal(game.Fingerprint, CustomMultiplierExample.CreateGame(firstGridMultiplier: 3).Fingerprint);
        Assert.NotEqual(game.Fingerprint, CustomMultiplierExample.CreateGame(firstGridMultiplier: 5).Fingerprint);
    }

    [Fact]
    public void Example_ShouldResumeWithTheSamePayoutAndRecordedDraws()
    {
        var game = CustomMultiplierExample.CreateGame();
        var request = new SpinRequest(EvaluationId: "custom-policy-resume", CalculationStakeUnits: 100);
        var recording = new RecordingDrawSource(new Tape(0, 0, 0, 0, 0, 0, 1, 1, 1));
        var expected = new SlotEngine().Evaluate(game, request, recording);
        Assert.Equal(700, expected.TotalPayoutUnits);
        Assert.Equal(9, recording.Snapshot().Length);

        var replay = new ReplayDrawSource(recording.Snapshot());
        var resumed = new SlotEngine().Evaluate(game, request, replay, maxGridEvaluations: 1);
        Assert.False(resumed.IsComplete);
        var steps = resumed.Steps.ToBuilder();
        while (resumed.Continuation is not null)
        {
            var checkpoint = SpinStateSerializer.DeserializeContinuation(SpinStateSerializer.Serialize(resumed.Continuation));
            // Recreate the policy too: resumption must depend on saved engine state alone.
            resumed = new SlotEngine().Resume(CustomMultiplierExample.CreateGame(), checkpoint, replay, maxGridEvaluations: 1);
            steps.AddRange(resumed.Steps);
        }

        var actual = resumed with { Steps = steps.ToImmutable() };
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }
}
