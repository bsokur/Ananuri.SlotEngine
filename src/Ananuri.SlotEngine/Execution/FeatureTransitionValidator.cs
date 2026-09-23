using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Execution;

internal static class FeatureTransitionValidator
{
    internal static void Validate(GameDefinition game, SpinContinuation state,
        FeatureTransition? transition, bool winLimitReached)
    {
        if (transition is null || transition.GrantedFreeSpins < 0
            || transition.GrantedFreeSpins > state.PendingFreeSpins)
            throw new InvalidOperationException("Feature policy returned an invalid free-spin grant.");

        if (winLimitReached || !game.Features.SupportsFreeSpins)
        {
            if (transition.Bonus is not null || transition.GrantedFreeSpins != 0)
                throw new InvalidOperationException("A capped round or disabled feature cannot return free spins.");
            return;
        }

        var request = state.Request;
        var previous = request.Bonus;
        long total = (long)(previous?.TotalSpinsAwarded ?? 0) + transition.GrantedFreeSpins;
        long completed = previous is null ? 0 : (long)previous.CompletedSpins + 1;
        long remaining = total - completed;
        var bonus = transition.Bonus;
        if (remaining < 0 || (bonus is null) != (remaining == 0))
            throw new InvalidOperationException("Feature policy discarded remaining free spins or returned an exhausted bonus.");
        if (bonus is null) return;

        long expectedMultiplier = previous is not null && game.BonusMultiplier.Persistence == MultiplierPersistence.Bonus
            ? state.Multiplier : game.BonusMultiplier.Start;
        if (string.IsNullOrWhiteSpace(bonus.OriginatingRoundId)
            || bonus.OriginatingRoundId != (previous?.OriginatingRoundId ?? request.EvaluationId)
            || bonus.GameFingerprint != game.Fingerprint
            || bonus.EngineVersion != SlotEngine.EngineVersion || bonus.RulesVersion != SlotEngine.RulesVersion
            || bonus.CalculationStakeUnits != request.CalculationStakeUnits
            || bonus.RoundPayoutUnits != state.RoundPayoutUnits
            || bonus.TotalSpinsAwarded <= 0 || bonus.CompletedSpins < 0 || bonus.RemainingSpins <= 0
            || bonus.TotalSpinsAwarded != total || bonus.CompletedSpins != completed
            || bonus.RemainingSpins != remaining || bonus.Multiplier != expectedMultiplier)
            throw new InvalidOperationException("Feature policy returned inconsistent bonus state.");

        try
        {
            game.Features.ValidateState(game, bonus);
        }
        catch (ArgumentException error)
        {
            throw new InvalidOperationException("Feature policy returned invalid bonus state.", error);
        }
    }
}
