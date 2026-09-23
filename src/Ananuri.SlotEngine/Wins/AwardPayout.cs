namespace Ananuri.SlotEngine.Wins;

/// <summary>An ordinary award with its applied grid multiplier, before any round cap reduction.</summary>
/// <param name="Award">Selected base award and its contributing positions.</param>
/// <param name="AppliedMultiplier">Positive feature multiplier applied to the base payout.</param>
/// <param name="RawPayoutUnits">Gross base payout times the applied multiplier, in integer currency units before capping.</param>
public sealed record AwardPayout(WinAward Award, long AppliedMultiplier, long RawPayoutUnits);
