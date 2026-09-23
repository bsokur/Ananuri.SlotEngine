namespace Ananuri.SlotEngine.Configuration.Packages;

/// <summary>One JSON refill-table entry with a numeric symbol ID and positive integer weight.</summary>
public sealed record WeightPackage(int Symbol, int Weight);
