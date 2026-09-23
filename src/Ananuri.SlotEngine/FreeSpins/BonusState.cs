using System.Text.Json.Serialization;

namespace Ananuri.SlotEngine.FreeSpins;

/// <summary>Trusted runtime state; never accept this directly from a player request.</summary>
/// <param name="OriginatingRoundId">Evaluation identifier of the paid spin that started this bonus.</param>
/// <param name="GameFingerprint">Fingerprint of the exact game definition used by the round.</param>
/// <param name="EngineVersion">Engine build that produced this state.</param>
/// <param name="RulesVersion">Execution rules version that produced this state.</param>
/// <param name="CalculationStakeUnits">Original paid stake in integer currency units, also used to calculate free-spin awards.</param>
/// <param name="RemainingSpins">Number of unplayed free spins, including the next one.</param>
/// <param name="TotalSpinsAwarded">Lifetime number of free spins granted to this bonus, including retriggers.</param>
/// <param name="CompletedSpins">Number of free spins already completed.</param>
/// <param name="Multiplier">Multiplier to start the next free spin; reset or persisted according to the bonus strategy.</param>
/// <param name="RoundPayoutUnits">Cumulative gross payout in integer currency units for the paid spin and all completed free spins.</param>
public sealed record BonusState(
    [property: JsonRequired] string OriginatingRoundId,
    [property: JsonRequired] string GameFingerprint,
    [property: JsonRequired] string EngineVersion,
    [property: JsonRequired] string RulesVersion,
    [property: JsonRequired] long CalculationStakeUnits,
    [property: JsonRequired] int RemainingSpins,
    [property: JsonRequired] int TotalSpinsAwarded,
    [property: JsonRequired] int CompletedSpins,
    [property: JsonRequired] long Multiplier,
    [property: JsonRequired] long RoundPayoutUnits);
