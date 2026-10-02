using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Execution;

internal static class SpinRequestValidator
{
    internal static void Validate(GameDefinition game, SpinRequest request)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EvaluationId);
        if (!game.Bets.Allows(request.CalculationStakeUnits)) throw new ArgumentException("Unsupported calculation stake.");
        if (request.Bonus is not null)
        {
            if (!game.Features.SupportsFreeSpins) throw new ArgumentException("This game has no free-spin feature.");
            game.Features.ValidateState(game, request.Bonus);
            if (request.CalculationStakeUnits != request.Bonus.CalculationStakeUnits)
                throw new ArgumentException("Free-spin calculation stake must match the triggering stake.");
        }
    }
}
