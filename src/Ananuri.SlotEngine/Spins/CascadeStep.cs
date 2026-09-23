using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Spins;

/// <summary>Evidence and payout accounting for one evaluated grid, before any following cascade.</summary>
/// <param name="GridIndex">Zero-based grid index within the spin; zero identifies the initial board.</param>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="Awards">Ordinary awards and their uncapped multiplied payouts for this grid.</param>
/// <param name="Scatter">Scatter evidence and base payout before multiplication.</param>
/// <param name="ScatterAppliedMultiplier">Multiplier applied to this grid's base scatter payout.</param>
/// <param name="MultiplierBefore">Multiplier state before processing this grid.</param>
/// <param name="AppliedMultiplier">Multiplier applied to this grid's ordinary awards.</param>
/// <param name="MultiplierAfter">Multiplier state retained after this grid.</param>
/// <param name="Collections">Symbol instances newly collected on this grid.</param>
/// <param name="RawPayoutUnits">Grid payout after multiplication, before the round cap.</param>
/// <param name="PayablePayoutUnits">Grid payout remaining after applying the round cap.</param>
/// <param name="CapAdjustmentUnits">Amount removed from the raw grid payout by the round cap.</param>
/// <param name="Transition">Removal, movement, and refill evidence producing the next board, or null if no cascade occurs.</param>
public sealed record CascadeStep(int GridIndex, SymbolBoard Grid, ImmutableArray<AwardPayout> Awards,
    ScatterResult Scatter, long ScatterAppliedMultiplier, long MultiplierBefore,
    long AppliedMultiplier, long MultiplierAfter, ImmutableArray<SymbolCollection> Collections,
    long RawPayoutUnits, long PayablePayoutUnits, long CapAdjustmentUnits, CascadeTransition? Transition);
