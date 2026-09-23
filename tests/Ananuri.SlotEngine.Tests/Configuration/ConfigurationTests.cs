using System.Text.Json;
using System.Text.Json.Nodes;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class ConfigurationTests
{
    private readonly SlotEngine _engine = new();

    [Fact]
    public void PaylinePackage_ShouldSelectTheConfiguredPoliciesAndData()
    {
        var game = LoadSample("PaylineGame.json");
        Assert.IsType<PaylineWinEvaluator>(game.Wins);
        Assert.IsType<ReelStripGridGenerator>(game.GridGenerator);
        Assert.IsType<NoCascades>(game.Cascades);
        Assert.IsType<NoWinLimit>(game.WinLimit);
        Assert.IsType<NoScatters>(game.Scatters);
        Assert.IsType<NoFreeSpins>(game.Features);
        Assert.Null(game.WinLimit.MaximumPayout(100));
        Assert.Equal(3, game.ReelCount);
        Assert.Equal(2, game.VisibleRows);
        Assert.Equal(new long[] { 100 }, game.Bets.AllowedTotalStakeUnits);
    }

    [Fact]
    public void NullCollectionEntry_ShouldProduceValidationError()
    {
        var json = JsonNode.Parse(ReadSampleJson("CollectedSymbolsGame.json"))!;
        json["bonusMultiplier"]!["collectionValues"] = new JsonArray((JsonNode?)null);
        var error = Assert.Throws<ArgumentException>(() => GamePackageLoader.Load(json.ToJsonString()));
        Assert.Contains("collection", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangedPackage_ShouldRejectHistoricalContinuation()
    {
        var game = Fixture();
        var part = _engine.Evaluate(game, new("id", 100), new Tape(0, 0, 0, 0, 1, 0), 1);
        var changed = Fixture(winLimit: 999);
        Assert.NotEqual(game.Fingerprint, changed.Fingerprint);
        Assert.Throws<ArgumentException>(() => _engine.Resume(changed, part.Continuation!, new Tape()));
        Assert.Throws<ArgumentException>(() => _engine.Resume(game, part.Continuation! with { EngineVersion = "other" }, new Tape()));
    }

    [Fact]
    public void Packages_ShouldLoadBothStrategiesAndCaptureConfiguration()
    {
        var first = LoadSample("CascadingGame.json");
        Assert.Equal(4, first.VisibleRows);
        Assert.IsType<PayingCascadeMultiplier>(first.BonusMultiplier);
        var collected = LoadSample("CollectedSymbolsGame.json");
        Assert.IsType<CollectedSymbolMultiplier>(collected.BonusMultiplier);
        Assert.NotEqual(first.Fingerprint, collected.Fingerprint);
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("profile", "\"unknown\"")]
    [InlineData("cascadePolicy", "\"unknown\"")]
    [InlineData("bonusMultiplier.strategy", "\"execute-script\"")]
    [InlineData("bonusMultiplier.increment", "0")]
    [InlineData("bonusMultiplier.maximum", "9223372036854775807")]
    [InlineData("wilds.substitutesFor", "[1,8]")]
    public void Package_ShouldRejectUnsupportedOrIncompatibleRules(string path, string replacementJson)
    {
        var json = JsonNode.Parse(ReadSampleJson("CascadingGame.json"))!;
        var segments = path.Split('.');
        var owner = json;
        foreach (var segment in segments[..^1]) owner = owner[segment]!;
        owner[segments[^1]] = JsonNode.Parse(replacementJson);
        Assert.ThrowsAny<ArgumentException>(() => GamePackageLoader.Load(json.ToJsonString()));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("visibleRows")]
    [InlineData("cascadePolicy")]
    [InlineData("matchCount")]
    [InlineData("integerEnum")]
    public void Package_ShouldRejectUnknownOrMissingPropertiesAndIntegerEnums(string invalid)
    {
        var json = JsonNode.Parse(ReadSampleJson("CascadingGame.json"))!.AsObject();
        switch (invalid)
        {
            case "unknown":
                json["bonusMultiplier"]!["incremnt"] = 1;
                break;
            case "visibleRows":
            case "cascadePolicy":
                Assert.True(json.Remove(invalid));
                break;
            case "matchCount":
                Assert.True(json["paytable"]![0]!.AsObject().Remove("matchCount"));
                break;
            default:
                json["bonusMultiplier"]!["persistence"] = 1;
                break;
        }
        Assert.Throws<JsonException>(() => GamePackageLoader.Load(json.ToJsonString()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Package_ShouldRejectDuplicateProperties(bool nested)
    {
        var json = JsonNode.Parse(ReadSampleJson("CascadingGame.json"))!;
        string valid = json.ToJsonString();
        string duplicate;
        if (nested)
        {
            string payline = json["paylines"]![0]!.ToJsonString();
            string repeated = payline.Insert(payline.Length - 1, ",\"id\":" + json["paylines"]![0]!["id"]!.ToJsonString());
            duplicate = valid.Replace(payline, repeated, StringComparison.Ordinal);
            Assert.NotEqual(valid, duplicate);
        }
        else duplicate = valid.Insert(valid.Length - 1, ",\"schemaVersion\":1");
        Assert.Throws<JsonException>(() => GamePackageLoader.Load(duplicate));
    }
}
