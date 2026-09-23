using System.Text.Json;
using System.Text.Json.Serialization;
using Ananuri.SlotEngine.FreeSpins;

namespace Ananuri.SlotEngine.Spins;

/// <summary>Strict JSON persistence for trusted runtime state. The engine validates game-specific invariants on use.</summary>
public static class SpinStateSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Serialize(SpinContinuation state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, Options);
    }

    public static string Serialize(BonusState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, Options);
    }

    /// <summary>Reads a complete checkpoint from strict JSON. Resume performs the game-specific validation.</summary>
    public static SpinContinuation DeserializeContinuation(string json) => Deserialize<SpinContinuation>(json);
    /// <summary>Reads complete bonus state from strict JSON. Evaluate performs the game-specific validation.</summary>
    public static BonusState DeserializeBonus(string json) => Deserialize<BonusState>(json);

    private static T Deserialize<T>(string json) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException("Persisted spin state must not be null.");
    }
}
