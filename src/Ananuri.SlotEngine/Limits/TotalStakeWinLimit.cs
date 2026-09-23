using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Limits;

/// <summary>Explicit gross payout cap for the paid spin and its complete associated bonus.</summary>
public sealed class TotalStakeWinLimit : IWinLimitPolicy
{
    public TotalStakeWinLimit(long multiplier)
    {
        if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier));
        Multiplier = multiplier;
    }
    /// <summary>Positive factor that converts the triggering stake to a gross round payout cap.</summary>
    public long Multiplier { get; }
    public string ConfigurationKey => $"total-stake-round-limit-v1:{Multiplier}";
    /// <exception cref="OverflowException">The stake multiplied by the cap factor exceeds integer payout storage.</exception>
    public long? MaximumPayout(long calculationStake) => checked(calculationStake * Multiplier);
    public void Validate(GameDefinition game)
    {
        if ((System.Numerics.BigInteger)game.Bets.AllowedTotalStakeUnits.Max() * Multiplier > long.MaxValue)
            throw new ArgumentException("The round limit exceeds payout storage.");
    }
}
