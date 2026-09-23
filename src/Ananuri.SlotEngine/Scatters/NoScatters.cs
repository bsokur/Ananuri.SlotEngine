using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Scatters;

/// <summary>Disables scatter payouts, scatter counting, and scatter-triggered free spins.</summary>
public sealed class NoScatters : IScatterEvaluator
{
    public ImmutableHashSet<SymbolId> Symbols => [];
    public bool CanAwardFreeSpins => false;
    public string ConfigurationKey => "no-scatters-v1";
    public long MaximumAwardMultiplier => 0;
    public void Validate(GameDefinition game) { }
    public ScatterResult Evaluate(GameDefinition game, ScatterContext context) => ScatterResult.None;
}
