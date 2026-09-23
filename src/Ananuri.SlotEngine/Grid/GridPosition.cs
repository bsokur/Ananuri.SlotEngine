namespace Ananuri.SlotEngine.Grid;

/// <summary>Zero-based reel and row coordinates; rows increase from top to bottom.</summary>
public readonly record struct GridPosition(int Reel, int Row);
