using System.Text.Json;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Tests.Fixtures;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.FixedPaylines;

public sealed class EngineTests
{
    private readonly ISlotEngine _engine = new SlotEngine();

    [Theory]
    [MemberData(nameof(ReferenceVectors))]
    public void Evaluate_ShouldMatchDocumentedGridAndPayout(int[] stops, long stake, string[] middleRow, long expected)
    {
        var result = Evaluate(LoadSample("FiveReelGame.json"), stake, stops);
        var step = Assert.Single(result.Steps);
        var symbols = new Dictionary<string, SymbolId>
        {
            ["A"] = A,
            ["B"] = B,
            ["C"] = C,
            ["D"] = new SymbolId(4)
        };
        Assert.Equal(middleRow.Select(symbol => symbols[symbol]),
            Enumerable.Range(0, step.Grid.ReelCount).Select(reel => step.Grid[reel, 1].Symbol));
        Assert.Equal(expected, result.TotalPayoutUnits);
        Assert.Equal(stake, result.ChargedStakeUnits);
        Assert.Equal(CompletionReason.NoMoreWins, result.CompletionReason);
        Assert.Null(step.Transition);
    }

    public static IEnumerable<object[]> ReferenceVectors()
    {
        using var stream = typeof(EngineTests).Assembly.GetManifestResourceStream("reference-vectors.json")
            ?? throw new InvalidOperationException("The documented reference vectors are missing.");
        using var document = JsonDocument.Parse(stream);
        foreach (var vector in document.RootElement.EnumerateArray())
            yield return new object[]
            {
                vector.GetProperty("stops").EnumerateArray().Select(value => value.GetInt32()).ToArray(),
                vector.GetProperty("stakeUnits").GetInt64(),
                vector.GetProperty("middleRow").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                vector.GetProperty("payoutUnits").GetInt64()
            };
    }

    [Fact]
    public void Evaluate_ShouldWrapReel_WhenStopIsAtEnd()
    {
        var result = Evaluate(LoadSample("FiveReelGame.json"), 100, [5, 0, 0, 0, 0]);
        Assert.Equal(new[] { B, B, A },
            Enumerable.Range(0, 3).Select(row => result.Steps[0].Grid[0, row].Symbol).ToArray());
    }

    [Fact]
    public void Evaluate_ShouldReportPositions_WhenLineWins()
    {
        var result = Evaluate(LoadSample("FiveReelGame.json"), 100, [0, 0, 0, 0, 0]);
        var win = Assert.Single(result.Steps[0].Awards).Award;
        Assert.Equal(new[] { new GridPosition(0, 1), new GridPosition(1, 1), new GridPosition(2, 1) }, win.Positions.ToArray());
    }

    [Fact]
    public void Evaluate_ShouldDivideStake_WhenTwoLinesAreActive()
    {
        var package = System.Text.Json.Nodes.JsonNode.Parse(ReadSampleJson("FiveReelGame.json"))!;
        package["paylines"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""{"id":1,"rows":[0,0,0,0,0]}"""));
        var result = Evaluate(GamePackageLoader.Load(package.ToJsonString()), 100, [0, 0, 0, 0, 0]);
        Assert.Equal(250L, result.TotalPayoutUnits);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Evaluate_ShouldRejectStop_WhenOutsideReel(int stop)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FixedReelStops(LoadSample("FiveReelGame.json"), [stop, 0, 0, 0, 0]));
    }

    [Fact]
    public void Evaluate_ShouldRejectInput_WhenStopCountDoesNotMatch()
    {
        Assert.Throws<ArgumentException>(() =>
            new FixedReelStops(LoadSample("FiveReelGame.json"), [0]));
    }

    [Fact]
    public void Evaluate_ShouldRejectStake_WhenNotConfigured()
    {
        var draws = new EngineFixtures.Tape();
        Assert.Throws<ArgumentException>(() => _engine.Evaluate(LoadSample("FiveReelGame.json"), new("invalid-stake", 101), draws));
        Assert.Equal(0, draws.Used);
    }

    [Fact]
    public void Evaluate_ShouldUseCapturedStops_WhenOriginalArrayChanges()
    {
        var stops = new[] { 0, 0, 0, 0, 0 };
        var game = LoadSample("FiveReelGame.json");
        var input = new FixedReelStops(game, stops);
        stops[0] = 1;
        Assert.Equal(500L, _engine.Evaluate(game, new("captured-stops", 100), input).TotalPayoutUnits);
    }

    [Fact]
    public void Evaluate_ShouldReplayEveryResult_WhenSameInputsAreRepeated()
    {
        var game = LoadSample("PaylineGame.json");
        for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
                for (int c = 0; c < 4; c++)
                {
                    var input = new FixedReelStops(game, [a, b, c]);
                    var request = new SpinRequest("repeat", 100);
                    var first = _engine.Evaluate(game, request, input);
                    var second = _engine.Evaluate(game, request, input);
                    Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
                    Assert.Equal(first.Steps[0].Awards.Sum(w => w.RawPayoutUnits), first.TotalPayoutUnits);
                }
    }

    [Fact]
    public void Evaluate_ShouldMatchIndependentDistribution_WhenTinyGameIsEnumerated()
    {
        var game = LoadSample("PaylineGame.json");
        var distribution = new Dictionary<long, int>();
        for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
                for (int c = 0; c < 4; c++)
                {
                    long payout = Evaluate(game, 100, [a, b, c]).TotalPayoutUnits;
                    distribution[payout] = distribution.GetValueOrDefault(payout) + 1;
                }
        // Independently derived: 2^3 A triples, one B triple, one C triple.
        Assert.Equal(new[] { (0L, 54), (400L, 2), (600L, 8) },
            distribution.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).ToArray());
        Assert.Equal(5600L, distribution.Sum(pair => pair.Key * pair.Value));
    }

    [Fact]
    public void Evaluate_ShouldUseShorterAward_WhenExactLengthIsMissing()
    {
        var a = A;
        var game = new GameDefinition("fallback", "1", 1, new[] { a },
            Enumerable.Range(0, 4).Select(_ => new ReelStrip(new[] { a })),
            new[] { new Payline(0, new[] { 0, 0, 0, 0 }) },
            new[] { new PaytableEntry(a, 3, 5) }, new BetDefinition(new long[] { 100 }));
        Assert.Equal(500L, Evaluate(game, 100, [0, 0, 0, 0]).TotalPayoutUnits);
    }

    private SpinEvaluation Evaluate(GameDefinition game, long stake, int[] stops) =>
        _engine.Evaluate(game, new("fixed-paylines", stake), new FixedReelStops(game, stops));
}
