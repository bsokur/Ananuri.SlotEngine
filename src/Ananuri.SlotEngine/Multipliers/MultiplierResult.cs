using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Multiplier applied to the current grid, state for the next grid, and newly collected instances.</summary>
/// <param name="AppliedMultiplier">Multiplier applied to this grid's ordinary awards, and to scatter awards when enabled by the game.</param>
/// <param name="NextState">Multiplier state for the next grid.</param>
/// <param name="Collections">Symbol instances newly collected on this grid.</param>
public sealed record MultiplierResult(long AppliedMultiplier, MultiplierState NextState,
    ImmutableArray<SymbolCollection> Collections);
