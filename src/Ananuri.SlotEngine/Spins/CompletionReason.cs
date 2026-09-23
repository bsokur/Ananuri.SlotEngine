namespace Ananuri.SlotEngine.Spins;

/// <summary>Explains why evaluation stopped; WorkBudget requires resuming the returned continuation.</summary>
public enum CompletionReason
{
    /// <summary>The cascade policy selected no further removals, so the spin completed.</summary>
    NoMoreWins,
    /// <summary>The paid-round payout cap was reached; no further cascades or free spins remain.</summary>
    WinLimit,
    /// <summary>The per-call grid budget was exhausted; resume the returned checkpoint to finish the spin.</summary>
    WorkBudget
}
