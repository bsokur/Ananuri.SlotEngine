namespace Ananuri.SlotEngine.Configuration.Packages;

/// <param name="Id">Non-negative line identifier, unique within the game.</param>
/// <param name="Rows">Zero-based selected row for each reel, ordered left to right.</param>
public sealed record PaylinePackage(int Id, int[] Rows);
