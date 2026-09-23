using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Wins;

/// <summary>Evaluates ordinary grid awards and declares the symbol roles and payout bounds it can produce.</summary>
public interface IWinEvaluator : IGameComponent
{
    /// <summary>Every symbol this evaluator can award as an ordinary paying symbol.</summary>
    ImmutableHashSet<SymbolId> GetPayingSymbols(GameDefinition game);
    /// <summary>All wild symbols and substitution targets used by this evaluator.</summary>
    ImmutableHashSet<SymbolId> SubstitutionSymbols { get; }
    /// <summary>
    /// Non-negative conservative bound on the sum of base ordinary awards for any grid at this stake,
    /// before feature multipliers or caps. Must hold in both spin modes. Its inputs must be represented
    /// by the game data or this component's ConfigurationKey.
    /// </summary>
    /// <param name="game">Game definition containing the evaluator's payout data.</param>
    /// <param name="calculationStake">Total stake in integer currency units used to calculate awards.</param>
    /// <returns>A conservative upper bound in integer currency units, including every ordinary award in a single grid.</returns>
    BigInteger MaximumBasePayout(GameDefinition game, long calculationStake);
    /// <summary>Produces the ordinary awards for one grid before feature multipliers or round caps.</summary>
    /// <param name="game">Game definition containing the evaluator's rules.</param>
    /// <param name="grid">Current board with stable symbol instance identifiers.</param>
    /// <param name="calculationStake">Total stake in integer currency units; also the original stake for free spins.</param>
    /// <returns>An initialized array of positive awards, or an empty array when the grid has no ordinary wins.</returns>
    ImmutableArray<WinAward> Evaluate(GameDefinition game, SymbolBoard grid, long calculationStake);
}
