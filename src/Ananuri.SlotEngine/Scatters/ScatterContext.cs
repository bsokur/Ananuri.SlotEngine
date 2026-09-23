using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Scatters;

/// <summary>Grid and prior counting state supplied to scatter evaluation.</summary>
/// <param name="Mode">Paid or free spin mode.</param>
/// <param name="GridIndex">Zero-based grid index within the spin; zero identifies the initial board.</param>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="CalculationStakeUnits">Positive total stake in integer accounting units used to calculate all awards.</param>
/// <param name="CountedInstanceIds">Previously counted scatter instances.</param>
/// <param name="FeatureAlreadyTriggered">Whether an earlier grid in this spin already requested free spins.</param>
public sealed record ScatterContext(SpinMode Mode, int GridIndex, SymbolBoard Grid, long CalculationStakeUnits,
    ImmutableHashSet<long> CountedInstanceIds, bool FeatureAlreadyTriggered);
