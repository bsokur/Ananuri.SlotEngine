using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Grid;

/// <summary>A symbol and its positive integer relative weight in a refill table.</summary>
public sealed record SymbolWeight(SymbolId Symbol, int Weight);
