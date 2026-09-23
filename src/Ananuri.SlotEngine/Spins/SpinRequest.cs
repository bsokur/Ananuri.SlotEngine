using System.Text.Json.Serialization;
using Ananuri.SlotEngine.FreeSpins;

namespace Ananuri.SlotEngine.Spins;

/// <summary>Input for one spin. The host supplies a unique evaluation ID and integer stake units; a bonus state selects a free spin.</summary>
/// <param name="EvaluationId">Host-assigned unique identifier for this spin evaluation, retained across resumptions.</param>
/// <param name="CalculationStakeUnits">Positive total stake in integer accounting units used to calculate all awards.</param>
/// <param name="Bonus">Trusted bonus state to consume for a free spin; null starts a paid spin.</param>
public sealed record SpinRequest(
    [property: JsonRequired] string EvaluationId,
    [property: JsonRequired] long CalculationStakeUnits,
    [property: JsonRequired] BonusState? Bonus = null)
{
    public SpinMode Mode => Bonus is null ? SpinMode.Paid : SpinMode.Free;
    /// <summary>The total stake for a paid spin, or zero for a free spin. This value does not perform a wallet charge.</summary>
    public long ChargedStakeUnits => Bonus is null ? CalculationStakeUnits : 0;
}
