using System.Text.Json;
using System.Text.Json.Serialization;
using Ananuri.SlotEngine.Configuration.Packages;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;

namespace Ananuri.SlotEngine.Configuration;

/// <summary>Strict data-only JSON loader for the built-in slot-engine-v1 component profile.</summary>
public static class GamePackageLoader
{
    public const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        AllowDuplicateProperties = false,
        Converters = { new JsonStringEnumConverter<MultiplierPersistence>(allowIntegerValues: false),
            new JsonStringEnumConverter<ScatterTiming>(allowIntegerValues: false) },
        MaxDepth = 32
    };

    /// <summary>Parses strict camel-case JSON, rejects unknown or duplicate fields, and compiles validated immutable game rules. Input is limited to 2,000,000 characters.</summary>
    public static GameDefinition Load(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (json.Length > 2_000_000) throw new ArgumentException("Game package exceeds the loader size limit.");
        var package = JsonSerializer.Deserialize<GamePackage>(json, Options) ?? throw new ArgumentException("Empty game package.");
        return Compile(package);
    }

    /// <summary>Copies transport configuration into immutable domain data and validates the built-in component profile.</summary>
    public static GameDefinition Compile(GamePackage package) => GamePackageCompiler.Compile(package);

}
