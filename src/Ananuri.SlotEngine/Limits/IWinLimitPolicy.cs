using Ananuri.SlotEngine.Configuration;

namespace Ananuri.SlotEngine.Limits;

/// <summary>Defines an optional cumulative gross payout cap for a paid spin and all of its free spins.</summary>
public interface IWinLimitPolicy : IGameComponent
{
    /// <summary>Maximum gross round payout, or null when the game has no payout cap.</summary>
    /// <param name="calculationStake">Original paid stake in integer currency units.</param>
    /// <returns>A positive cap in integer currency units, or null for no gameplay cap.</returns>
    long? MaximumPayout(long calculationStake);
}
