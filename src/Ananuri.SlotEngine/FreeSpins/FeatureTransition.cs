namespace Ananuri.SlotEngine.FreeSpins;

/// <summary>Validated free-spin grants and optional bonus state after a completed spin.</summary>
/// <param name="Bonus">State for the next free spin, or null when no bonus remains or the round reached its payout cap.</param>
/// <param name="GrantedFreeSpins">New spins granted by this spin after any lifetime allowance; zero when capped.</param>
public sealed record FeatureTransition(BonusState? Bonus, int GrantedFreeSpins);
