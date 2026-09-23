namespace Ananuri.SlotEngine.Configuration.Packages;

/// <summary>One JSON mapping from a collection symbol to its positive multiplier increment.</summary>
public sealed record CollectionPackage(int Symbol, long Value);
