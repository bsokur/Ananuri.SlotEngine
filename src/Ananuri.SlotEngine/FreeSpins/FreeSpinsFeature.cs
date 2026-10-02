using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.FreeSpins;

/// <summary>Grants and retriggers free spins within a fixed lifetime allowance for each paid round.</summary>
public sealed class FreeSpinsFeature : IFeaturePolicy
{
    public bool SupportsFreeSpins => true;
    public FreeSpinsFeature(int maximumAwardedSpins)
    {
        if (maximumAwardedSpins <= 0) throw new ArgumentOutOfRangeException(nameof(maximumAwardedSpins));
        MaximumAwardedSpins = maximumAwardedSpins;
    }
    /// <summary>Maximum lifetime free-spin count, including retriggers, for one paid round.</summary>
    public int MaximumAwardedSpins { get; }
    public string ConfigurationKey => $"free-spins-v1:{MaximumAwardedSpins}";
    public void Validate(GameDefinition game) { }
    public void ValidateState(GameDefinition game, BonusState state)
    {
        if (state.GameFingerprint != game.Fingerprint || state.EngineVersion != SlotEngine.EngineVersion
            || state.RulesVersion != SlotEngine.RulesVersion || string.IsNullOrWhiteSpace(state.OriginatingRoundId)
            || !game.Bets.Allows(state.CalculationStakeUnits)
            || state.TotalSpinsAwarded <= 0 || state.TotalSpinsAwarded > MaximumAwardedSpins
            || state.CompletedSpins < 0 || state.CompletedSpins >= state.TotalSpinsAwarded
            || state.RemainingSpins != state.TotalSpinsAwarded - state.CompletedSpins
            || state.Multiplier < game.BonusMultiplier.Start || state.Multiplier > game.BonusMultiplier.Maximum
            || (game.BonusMultiplier.Persistence == MultiplierPersistence.Spin && state.Multiplier != game.BonusMultiplier.Start)
            || state.RoundPayoutUnits < 0
            || (game.WinLimit.MaximumPayout(state.CalculationStakeUnits) is long limit && state.RoundPayoutUnits >= limit))
            throw new ArgumentException("Bonus state is invalid or belongs to a different package/engine.", nameof(state));
    }
    public FeatureTransition Complete(GameDefinition game, SpinRequest request,
        long pendingFreeSpins, long multiplier, long roundPayout, bool winLimitReached)
    {
        if (winLimitReached) return new(Bonus: null, GrantedFreeSpins: 0);

        var previousBonus = request.Bonus;
        int grantedFreeSpins = (int)Math.Min(pendingFreeSpins, MaximumAwardedSpins - (previousBonus?.TotalSpinsAwarded ?? 0));
        int totalSpinsAwarded = (previousBonus?.TotalSpinsAwarded ?? 0) + grantedFreeSpins;
        // Completing the triggering paid spin grants a bonus without consuming one of its free spins.
        int completedSpins = previousBonus is null ? 0 : previousBonus.CompletedSpins + 1;
        int remainingSpins = totalSpinsAwarded - completedSpins;
        if (remainingSpins == 0) return new(Bonus: null, GrantedFreeSpins: grantedFreeSpins);

        long nextMultiplier = previousBonus is not null && game.BonusMultiplier.Persistence == MultiplierPersistence.Bonus
            ? multiplier : game.BonusMultiplier.Start;
        var nextBonus = new BonusState(
            OriginatingRoundId: previousBonus?.OriginatingRoundId ?? request.EvaluationId,
            GameFingerprint: game.Fingerprint,
            EngineVersion: SlotEngine.EngineVersion,
            RulesVersion: SlotEngine.RulesVersion,
            CalculationStakeUnits: request.CalculationStakeUnits,
            RemainingSpins: remainingSpins,
            TotalSpinsAwarded: totalSpinsAwarded,
            CompletedSpins: completedSpins,
            Multiplier: nextMultiplier,
            RoundPayoutUnits: roundPayout);
        return new(Bonus: nextBonus, GrantedFreeSpins: grantedFreeSpins);
    }
}
