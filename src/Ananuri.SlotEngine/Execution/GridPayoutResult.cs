using System.Collections.Immutable;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Execution;

internal sealed record GridPayoutResult(
    ImmutableArray<AwardPayout> Awards,
    long ScatterAppliedMultiplier,
    long RawPayoutUnits,
    long PayablePayoutUnits,
    long SpinPayoutUnits,
    long RoundPayoutUnits,
    bool WinLimitReached)
{
    internal long CapAdjustmentUnits => RawPayoutUnits - PayablePayoutUnits;
}
