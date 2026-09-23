using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Read-only grid evidence supplied to a multiplier strategy before applying the grid award.</summary>
/// <param name="Mode">Paid or free spin mode.</param>
/// <param name="GridIndex">Zero-based grid index within the spin; zero identifies the initial board.</param>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="Wins">Base ordinary awards for this grid, before applying its multiplier; excludes scatter awards.</param>
/// <param name="CollectedInstanceIds">Instance IDs already collected in this spin, preventing collection again after movement.</param>
public sealed record MultiplierContext(SpinMode Mode, int GridIndex, SymbolBoard Grid,
    ImmutableArray<WinAward> Wins, ImmutableHashSet<long> CollectedInstanceIds);
