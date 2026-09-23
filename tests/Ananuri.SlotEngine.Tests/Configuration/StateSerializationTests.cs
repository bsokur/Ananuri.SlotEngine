using System.Text.Json;
using System.Text.Json.Nodes;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class StateSerializationTests
{
    [Fact]
    public void MissingCheckpointFields_ShouldBeRejected_IncludingZeroValuedFields()
    {
        var game = Fixture();
        var result = new SlotEngine().Evaluate(game, new("paid", 100), new Tape(0, 0, 0, 0, 1, 0), 1);
        var state = result.Continuation!;
        Assert.Equal(2, state.PendingFreeSpins);
        var json = JsonNode.Parse(SpinStateSerializer.Serialize(state))!.AsObject();
        foreach (var field in json.Select(p => p.Key).ToArray())
        {
            var incomplete = json.DeepClone().AsObject();
            Assert.True(incomplete.Remove(field));
            Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeContinuation(incomplete.ToJsonString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SpinContinuation>(incomplete.ToJsonString()));
        }
        var restored = SpinStateSerializer.DeserializeContinuation(json.ToJsonString());
        var complete = new SlotEngine().Resume(game, restored, new Tape());
        Assert.Equal(2, complete.GrantedFreeSpins);
    }

    [Fact]
    public void MissingBonusFields_ShouldBeRejected_AndValidStateShouldRoundTrip()
    {
        var state = Bonus(Fixture());
        var json = JsonNode.Parse(SpinStateSerializer.Serialize(state))!.AsObject();
        foreach (var field in json.Select(p => p.Key).ToArray())
        {
            var incomplete = json.DeepClone().AsObject();
            incomplete.Remove(field);
            Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeBonus(incomplete.ToJsonString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BonusState>(incomplete.ToJsonString()));
        }
        Assert.Equal(state, SpinStateSerializer.DeserializeBonus(json.ToJsonString()));
    }

    [Theory]
    [InlineData("Request", "Bonus")]
    [InlineData("Request", "CalculationStakeUnits")]
    [InlineData("Bonus", "CompletedSpins")]
    [InlineData("Grid", "ReelCount")]
    [InlineData("Cell", "Id")]
    [InlineData("Cell", "Symbol")]
    [InlineData("Symbol", "Value")]
    public void MissingNestedFields_ShouldBeRejected(string owner, string field)
    {
        var game = Fixture();
        var part = new SlotEngine().Evaluate(game, new("free", 100, Bonus(game)), new Zeros(), 1);
        var json = JsonNode.Parse(SpinStateSerializer.Serialize(part.Continuation!))!;
        var target = owner switch
        {
            "Request" => json["Request"],
            "Bonus" => json["Request"]!["Bonus"],
            "Grid" => json["Grid"],
            "Cell" => json["Grid"]!["Cells"]![0],
            _ => json["Grid"]!["Cells"]![0]!["Symbol"]
        };
        Assert.True(target!.AsObject().Remove(field));
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeContinuation(json.ToJsonString()));
    }

    [Fact]
    public void NullAndUnknownStateFields_ShouldBeRejected()
    {
        var json = JsonNode.Parse(SpinStateSerializer.Serialize(Bonus(Fixture())))!;
        json["RoundPayotUnits"] = 123;
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeBonus(json.ToJsonString()));
        json.AsObject().Remove("RoundPayotUnits");
        json["OriginatingRoundId"] = null;
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeBonus(json.ToJsonString()));
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeBonus("null"));
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeContinuation("null"));
        var valid = SpinStateSerializer.Serialize(Bonus(Fixture()));
        var duplicate = valid.Insert(1, "\"RoundPayoutUnits\":123,");
        Assert.Throws<JsonException>(() => SpinStateSerializer.DeserializeBonus(duplicate));
    }
}
