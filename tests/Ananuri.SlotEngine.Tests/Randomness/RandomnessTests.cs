using System.Text.Json;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Randomness;

public sealed class RandomnessTests
{
    private readonly SlotEngine _engine = new();

    [Theory]
    [InlineData(1, "initial/reel/0", 4)]
    [InlineData(0, "initial/reel/1", 4)]
    [InlineData(0, "initial/reel/0", 5)]
    [InlineData(0, "refill/reel/0/row/0", 4)]
    public void FixedStops_ShouldRejectRequestsThatDoNotMatchCapturedInitialReels(long ordinal, string purpose, int upperBound)
    {
        var draws = new FixedReelStops(Fixture(), [0, 0, 0]);
        Assert.Throws<InvalidOperationException>(() => draws.Next(new("fixed", ordinal, purpose, upperBound)));
    }

    [Fact]
    public void FixedStops_ShouldValidateAgainstTheSelectedModesReelLengths()
    {
        var game = new GameDefinition("fixed-modes", "1", 1, [A, B], [new ReelStrip([A])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 5)], new BetDefinition([100]),
            features: new FreeSpinsFeature(10), freeSpinReels: [new ReelStrip([A, B])]);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedReelStops(game, [1]));
        var result = _engine.Evaluate(game, new("free-mode", 100, Bonus(game)), new FixedReelStops(game, [1], SpinMode.Free));
        Assert.Equal(B, Assert.Single(result.Steps).Grid[0, 0].Symbol);
        Assert.Equal(0, result.ChargedStakeUnits);
        Assert.Equal(0, result.TotalPayoutUnits);
    }

    [Fact]
    public void Recording_ShouldReuseAllocationOnDuplicateEvaluation()
    {
        var game = Fixture();
        var tape = new Tape(0, 0, 0, 0, 1, 0);
        var recording = new RecordingDrawSource(tape);
        var request = new SpinRequest("same-id", 100);
        var first = _engine.Evaluate(game, request, recording);
        var second = _engine.Evaluate(game, request, recording);
        Assert.Equal(6, tape.Used);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Throws<InvalidOperationException>(() => recording.Next(recording.Snapshot()[0].Request with { ExclusiveUpperBound = 99 }));
    }

    [Fact]
    public void Replay_ShouldRejectMissingOrMismatchedDraws()
    {
        var request = new DrawRequest("id", 0, "initial/reel/0", 4);
        var replay = new ReplayDrawSource([new(request, 2)]);
        Assert.Equal(2, replay.Next(request));
        Assert.Throws<InvalidOperationException>(() => replay.Next(request with { Purpose = "refill" }));
        Assert.Throws<InvalidOperationException>(() => replay.Next(request with { Ordinal = 1 }));
        Assert.Throws<ArgumentException>(() => new ReplayDrawSource([new(request, 4)]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void RandomDrawOutsideBound_ShouldFailWithoutBecomingLoss(int value) =>
        Assert.Throws<InvalidOperationException>(() => _engine.Evaluate(Fixture(), new("id", 100), new Tape(value)));
}
