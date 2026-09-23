using System.Text.Json.Serialization;

namespace Ananuri.SlotEngine.Definitions;

/// <summary>A non-negative symbol identifier scoped to a game; visual assets and display names belong to the host.</summary>
public readonly record struct SymbolId([property: JsonRequired] int Value);
