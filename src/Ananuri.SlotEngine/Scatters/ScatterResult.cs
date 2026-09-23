using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Scatters;

/// <summary>Scatter counting evidence, base payout, and requested free spins before feature limits are applied.</summary>
/// <param name="Count">Eligible scatter count used to select an award threshold.</param>
/// <param name="RequestedFreeSpins">Free spins requested before applying feature lifetime limits.</param>
/// <param name="BasePayoutUnits">Scatter payout in integer accounting units before the grid multiplier and round cap.</param>
/// <param name="Positions">Eligible scatter positions contributing to the count.</param>
/// <param name="CountedInstanceIds">All newly observed scatter instance IDs to mark as counted, including extra scatters excluded by the one-per-reel rule.</param>
public sealed record ScatterResult(int Count, int RequestedFreeSpins, long BasePayoutUnits,
    ImmutableArray<GridPosition> Positions, ImmutableArray<long> CountedInstanceIds)
{
    public static ScatterResult None { get; } = new(0, 0, 0, [], []);
}
