namespace Ananuri.SlotEngine.Randomness;

/// <summary>Identifies one bounded integer draw. Replay requires the evaluation ID, ordinal, purpose, and bound to match.</summary>
/// <param name="EvaluationId">Host-assigned unique identifier for this spin evaluation, retained across resumptions.</param>
/// <param name="Ordinal">Non-negative ordinal within this evaluation's draw sequence.</param>
/// <param name="Purpose">Stable semantic identity of the draw, such as its initial reel or refill cell.</param>
/// <param name="ExclusiveUpperBound">Positive exclusive upper bound; valid draw values begin at zero.</param>
public sealed record DrawRequest(string EvaluationId, long Ordinal, string Purpose, int ExclusiveUpperBound);
