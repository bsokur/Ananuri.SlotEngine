using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Wins;

/// <summary>A selected ordinary win before grid multipliers and cumulative round caps.</summary>
/// <param name="PaylineId">Configured payline identifier, or null for an evaluator that does not use paylines.</param>
/// <param name="Symbol">Declared paying symbol represented by this award.</param>
/// <param name="MatchCount">Number of contributing matches used to select the award.</param>
/// <param name="PaytableMultiplier">Paytable multiplier selected for this award.</param>
/// <param name="BasePayoutUnits">Positive gross payout in integer currency units before any feature multiplier or cap.</param>
/// <param name="Positions">Unique contributing board positions.</param>
/// <param name="WildPositions">Contributing positions whose symbols substitute for the paying symbol; a subset of Positions.</param>
public sealed record WinAward(int? PaylineId, SymbolId Symbol, int MatchCount,
    long PaytableMultiplier, long BasePayoutUnits, ImmutableArray<GridPosition> Positions,
    ImmutableArray<GridPosition> WildPositions);
