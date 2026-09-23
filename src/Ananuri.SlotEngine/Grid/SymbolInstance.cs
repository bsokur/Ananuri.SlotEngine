using System.Text.Json.Serialization;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Grid;

/// <summary>A symbol with an evaluation-local identity that survives movement between grids.</summary>
/// <param name="Id">Non-negative evaluation-local identity, retained when this instance moves.</param>
/// <param name="Symbol">Game-scoped symbol identifier.</param>
public sealed record SymbolInstance([property: JsonRequired] long Id, [property: JsonRequired] SymbolId Symbol);
