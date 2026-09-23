using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine;

/// <summary>Evaluates one paid or free spin at a time from immutable game data and caller-owned random draws.</summary>
public interface ISlotEngine
{
    /// <summary>Starts one spin and evaluates at most maxGridEvaluations grids. The host owns charging, settlement, durable draws, and bonus persistence.</summary>
    /// <param name="game">Validated immutable rules for this evaluation.</param>
    /// <param name="request">Spin identity, calculation stake, and optional trusted bonus state.</param>
    /// <param name="draws">Draw source covering all requested initial and refill draws.</param>
    /// <param name="maxGridEvaluations">Positive grid budget for this call, including the initial grid.</param>
    /// <returns>A complete spin or a continuation at the next grid boundary. TotalPayoutUnits is cumulative for the spin.</returns>
    /// <exception cref="ArgumentException">The request, bonus state, or work budget is invalid.</exception>
    /// <exception cref="InvalidOperationException">A component violates its output contract or the draw source cannot fulfill a request.</exception>
    SpinEvaluation Evaluate(GameDefinition game, SpinRequest request,
        IRandomDrawSource draws, int maxGridEvaluations = 1_000);
    /// <summary>Continues a saved spin at its next grid boundary using matching game rules and draw allocation.</summary>
    /// <param name="game">The exact game whose fingerprint is recorded in the continuation.</param>
    /// <param name="continuation">Trusted persisted checkpoint from Evaluate or Resume.</param>
    /// <param name="draws">The same logical draw allocation, beginning at the saved next ordinal.</param>
    /// <param name="maxGridEvaluations">Positive number of additional grids this call may evaluate.</param>
    /// <returns>Newly evaluated steps and cumulative spin and round totals; do not sum those totals across calls.</returns>
    /// <exception cref="ArgumentException">Checkpoint identity, version, state, or work budget is invalid.</exception>
    /// <exception cref="InvalidOperationException">A component violates its output contract or a draw is unavailable.</exception>
    SpinEvaluation Resume(GameDefinition game, SpinContinuation continuation,
        IRandomDrawSource draws, int maxGridEvaluations = 1_000);
}
