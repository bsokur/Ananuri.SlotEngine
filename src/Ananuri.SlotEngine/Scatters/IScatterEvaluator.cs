using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Scatters;

/// <summary>Evaluates scatter payouts and requested free spins independently of ordinary awards.</summary>
public interface IScatterEvaluator : IGameComponent
{
    /// <summary>All symbols counted as scatters by this evaluator.</summary>
    ImmutableHashSet<SymbolId> Symbols { get; }
    /// <summary>Whether any evaluation can request free spins.</summary>
    bool CanAwardFreeSpins { get; }
    /// <summary>Non-negative bound on base scatter payout divided by calculation stake, in either mode.</summary>
    long MaximumAwardMultiplier { get; }
    /// <summary>Returns payout and count evidence for eligible scatter instances; requested free spins are constrained later by the feature policy.</summary>
    ScatterResult Evaluate(GameDefinition game, ScatterContext context);
}
