using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.FreeSpins;

/// <summary>Controls free-spin grants and validates the bonus lifecycle after each completed spin.</summary>
public interface IFeaturePolicy : IGameComponent
{
    /// <summary>Whether the policy accepts free-spin grants and bonus requests.</summary>
    bool SupportsFreeSpins { get; }
    /// <summary>Validates a bonus against the game and the policy's lifetime and state restrictions.</summary>
    /// <remarks>Called for incoming bonus requests and outgoing non-null bonus states.</remarks>
    /// <param name="game">Game definition whose rules govern the bonus.</param>
    /// <param name="state">Trusted bonus state to validate before further execution or publication.</param>
    /// <exception cref="ArgumentException">The bonus state is invalid for this policy or game.</exception>
    void ValidateState(GameDefinition game, BonusState state);
    /// <summary>Completes a spin and grants between zero and the accumulated pending free spins.</summary>
    /// <remarks>
    /// A paid spin starts a bonus with zero completed spins; a free spin completes exactly one existing spin.
    /// Grants increase the lifetime total. Return a bonus exactly when unplayed spins remain, preserving the
    /// round identity, game and engine versions, calculation stake, and supplied cumulative round payout.
    /// The multiplier starts at the bonus strategy's start for a new bonus or spin persistence; otherwise
    /// preserve the supplied multiplier. A reached win limit discards all grants and remaining spins:
    /// return a null bonus and zero granted spins. The engine checks these invariants before returning results.
    /// </remarks>
    /// <param name="game">Game definition whose rules govern the completed spin.</param>
    /// <param name="request">Original request for this spin, including any incoming bonus.</param>
    /// <param name="pendingFreeSpins">Nonnegative requested grants accumulated over all evaluated grids in this spin.</param>
    /// <param name="multiplier">Multiplier state after the final grid evaluation.</param>
    /// <param name="roundPayout">Cumulative gross payout in integer currency units, including the triggering paid spin.</param>
    /// <param name="winLimitReached">Whether the paid round reached its configured cumulative payout cap.</param>
    /// <returns>A non-null transition with an optional next bonus and the actual number of newly granted spins.</returns>
    FeatureTransition Complete(GameDefinition game, SpinRequest request,
        long pendingFreeSpins, long multiplier, long roundPayout, bool winLimitReached);
}
