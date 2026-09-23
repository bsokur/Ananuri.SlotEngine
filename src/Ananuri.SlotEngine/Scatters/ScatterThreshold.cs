namespace Ananuri.SlotEngine.Scatters;

/// <summary>Award for reaching a minimum eligible scatter count; the highest satisfied threshold is selected.</summary>
/// <param name="MinimumCount">Minimum eligible scatter count for this award.</param>
/// <param name="FreeSpins">Non-negative number of requested free spins.</param>
/// <param name="TotalStakeMultiplier">Non-negative multiplier of the total calculation stake paid as a scatter award.</param>
public sealed record ScatterThreshold(int MinimumCount, int FreeSpins, long TotalStakeMultiplier = 0);
