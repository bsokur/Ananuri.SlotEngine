using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Execution;

internal static class SpinContinuationValidator
{
    internal static void Validate(GameDefinition game, SpinContinuation state)
    {
        var strategy = state.Request.Mode == SpinMode.Paid ? game.PaidMultiplier : game.BonusMultiplier;
        long originalPayout = state.Request.Bonus?.RoundPayoutUnits ?? 0;
        if (state.GameFingerprint != game.Fingerprint || state.EngineVersion != SlotEngine.EngineVersion
            || state.RulesVersion != SlotEngine.RulesVersion || state.NextGridIndex < 0 || state.NextDrawOrdinal < 0
            || state.Multiplier < strategy.Start || state.Multiplier > strategy.Maximum
            || state.SpinPayoutUnits < 0 || state.RoundPayoutUnits < originalPayout
            || (game.WinLimit.MaximumPayout(state.Request.CalculationStakeUnits) is long limit && state.RoundPayoutUnits >= limit)
            || state.RoundPayoutUnits - originalPayout != state.SpinPayoutUnits
            || state.PendingFreeSpins < 0 || state.CollectedInstanceIds is null || state.CountedScatterInstanceIds is null
            || state.CollectedInstanceIds.Any(id => id < 0 || id >= state.NextInstanceId)
            || state.CountedScatterInstanceIds.Any(id => id < 0 || id >= state.NextInstanceId)
            || state.InitialStops.IsDefault || state.InitialStops.Length != game.ReelCount)
            throw new ArgumentException("Invalid continuation or package/engine mismatch.");
        var reels = state.Request.Mode == SpinMode.Paid ? game.Reels : game.FreeSpinReels;
        for (int reel = 0; reel < reels.Length; reel++)
            if (state.InitialStops[reel] < 0 || state.InitialStops[reel] >= reels[reel].Length)
                throw new ArgumentException("Invalid continuation reel stop.");
        SymbolBoardValidator.Validate(game, state.Grid, state.NextInstanceId);
    }
}
