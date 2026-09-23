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
        if (winLimitReached) return new(null, 0);
        var previous = request.Bonus;
        int grant = (int)Math.Min(pendingFreeSpins, MaximumAwardedSpins - (previous?.TotalSpinsAwarded ?? 0));
        int total = (previous?.TotalSpinsAwarded ?? 0) + grant;
        int completed = previous is null ? 0 : previous.CompletedSpins + 1;
        int remaining = total - completed;
        if (remaining == 0) return new(null, grant);
        long nextMultiplier = previous is not null && game.BonusMultiplier.Persistence == MultiplierPersistence.Bonus
            ? multiplier : game.BonusMultiplier.Start;
        return new(new BonusState(previous?.OriginatingRoundId ?? request.EvaluationId, game.Fingerprint,
            SlotEngine.EngineVersion, SlotEngine.RulesVersion, request.CalculationStakeUnits,
            remaining, total, completed, nextMultiplier, roundPayout), grant);
    }
}
