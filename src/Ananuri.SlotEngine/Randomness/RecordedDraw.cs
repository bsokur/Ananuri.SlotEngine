namespace Ananuri.SlotEngine.Randomness;

/// <summary>A draw request paired with its allocated value for exact replay.</summary>
/// <param name="Request">Exact request that allocated this value.</param>
/// <param name="Value">Allocated integer in [0, Request.ExclusiveUpperBound).</param>
public sealed record RecordedDraw(DrawRequest Request, int Value);
