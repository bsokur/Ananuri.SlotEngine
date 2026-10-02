using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Execution;

internal static class GridPayoutCalculator
{
    internal static GridPayoutResult Calculate(GameDefinition game, SpinContinuation state,
        ImmutableArray<WinAward> wins, long appliedMultiplier, long scatterBasePayoutUnits)
    {
        long scatterAppliedMultiplier = game.MultiplyScatterAwards ? appliedMultiplier : 1;
        long rawPayoutUnits = checked(scatterBasePayoutUnits * scatterAppliedMultiplier);
        var awards = ImmutableArray.CreateBuilder<AwardPayout>();
        foreach (var win in wins)
        {
            long awardPayoutUnits = checked(win.BasePayoutUnits * appliedMultiplier);
            rawPayoutUnits = checked(rawPayoutUnits + awardPayoutUnits);
            awards.Add(new(Award: win, AppliedMultiplier: appliedMultiplier, RawPayoutUnits: awardPayoutUnits));
        }

        // The allowance covers the paid spin and all free spins in its round.
        long? maximumRoundPayoutUnits = game.WinLimit.MaximumPayout(state.Request.CalculationStakeUnits);
        long? remainingRoundAllowance = maximumRoundPayoutUnits is long limit ? limit - state.RoundPayoutUnits : null;
        long payablePayoutUnits = remainingRoundAllowance is long allowance ? Math.Min(rawPayoutUnits, allowance) : rawPayoutUnits;
        long roundPayoutUnits = checked(state.RoundPayoutUnits + payablePayoutUnits);
        long spinPayoutUnits = checked(state.SpinPayoutUnits + payablePayoutUnits);
        bool winLimitReached = remainingRoundAllowance.HasValue && payablePayoutUnits == remainingRoundAllowance.Value;

        return new(
            Awards: awards.ToImmutable(),
            ScatterAppliedMultiplier: scatterAppliedMultiplier,
            RawPayoutUnits: rawPayoutUnits,
            PayablePayoutUnits: payablePayoutUnits,
            SpinPayoutUnits: spinPayoutUnits,
            RoundPayoutUnits: roundPayoutUnits,
            WinLimitReached: winLimitReached);
    }
}
