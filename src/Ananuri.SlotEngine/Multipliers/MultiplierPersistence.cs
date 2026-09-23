namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Controls whether multiplier state resets per spin or carries across the awarded bonus.</summary>
public enum MultiplierPersistence
{
    /// <summary>Reset to the configured start at the beginning of each spin.</summary>
    Spin,
    /// <summary>Carry the multiplier across free spins in the same bonus; the first free spin starts at the bonus strategy's start.</summary>
    Bonus
}
