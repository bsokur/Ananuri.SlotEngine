using System.Collections.Immutable;
using Ananuri.SlotEngine.FreeSpins;

namespace Ananuri.SlotEngine.Spins;

/// <summary>Outcome of one evaluation call. Payout totals accumulate across resumptions, while Steps contains only the grids evaluated by this call.</summary>
/// <param name="EvaluationId">Host-assigned unique identifier for this spin evaluation, retained across resumptions.</param>
/// <param name="GameId">Host-defined game identifier.</param>
/// <param name="MathVersion">Host-defined version of the game's mathematical rules.</param>
/// <param name="GameFingerprint">Canonical game fingerprint binding state and results to exact rules.</param>
/// <param name="EngineVersion">Engine implementation version required for matching persisted state.</param>
/// <param name="RulesVersion">Engine rules version required for matching persisted state.</param>
/// <param name="Mode">Paid or free spin mode.</param>
/// <param name="ChargedStakeUnits">Total stake charged for a paid spin, or zero for a free spin; this is metadata, not a wallet operation.</param>
/// <param name="CalculationStakeUnits">Positive total stake in integer accounting units used to calculate all awards.</param>
/// <param name="StartingBonus">Trusted bonus state supplied at the beginning of this free spin, or null for a paid spin.</param>
/// <param name="InitialStops">Zero-based initial reel stops, one per reel in left-to-right order.</param>
/// <param name="Steps">Only the grid steps evaluated in this call; earlier continuation steps are not repeated.</param>
/// <param name="TotalPayoutUnits">Cumulative payable payout for this spin, including grids evaluated before a continuation.</param>
/// <param name="RoundPayoutUnits">Cumulative payable payout for the paid spin and all free spins in its round.</param>
/// <param name="GrantedFreeSpins">Free spins actually granted at completion after feature limits; zero while evaluation is incomplete.</param>
/// <param name="NextBonus">State for the next free spin after this spin completes; null while incomplete or when no bonus remains.</param>
/// <param name="CompletionReason">Reason evaluation stopped.</param>
/// <param name="Continuation">Checkpoint for the next grid when the work budget is exhausted; null once this spin completes.</param>
public sealed record SpinEvaluation(string EvaluationId, string GameId, string MathVersion,
    string GameFingerprint, string EngineVersion, string RulesVersion, SpinMode Mode,
    long ChargedStakeUnits, long CalculationStakeUnits, BonusState? StartingBonus,
    ImmutableArray<int> InitialStops, ImmutableArray<CascadeStep> Steps,
    long TotalPayoutUnits, long RoundPayoutUnits, int GrantedFreeSpins, BonusState? NextBonus,
    CompletionReason CompletionReason, SpinContinuation? Continuation)
{
    public bool IsComplete => Continuation is null;
}
