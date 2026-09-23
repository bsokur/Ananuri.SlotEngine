using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Advances a capped multiplier after eligible paying grids; each grid uses the value before its increment.</summary>
public sealed class PayingCascadeMultiplier : IMultiplierStrategy
{
    public PayingCascadeMultiplier(long start, long increment, long maximum,
        MultiplierPersistence persistence, bool includeInitialGrid = true)
    {
        if (start <= 0 || maximum < start || increment <= 0 || !Enum.IsDefined(persistence))
            throw new ArgumentException("Invalid paying-cascade multiplier configuration.");
        Start = start; Increment = increment; Maximum = maximum;
        Persistence = persistence; IncludeInitialGrid = includeInitialGrid;
    }
    public long Start { get; }
    public ImmutableHashSet<SymbolId> CollectionSymbols => [];
    /// <summary>Positive increment after each eligible paying grid, capped at Maximum.</summary>
    public long Increment { get; }
    public long Maximum { get; }
    public MultiplierPersistence Persistence { get; }
    /// <summary>Whether an initial-grid ordinary win advances the multiplier for the next grid; scatter awards do not qualify.</summary>
    public bool IncludeInitialGrid { get; }
    public string ConfigurationKey => $"paying-cascade-multiplier-v1:{Start}:{Increment}:{Maximum}:{Persistence}:{IncludeInitialGrid}";
    public void Validate(GameDefinition game) { }
    public MultiplierResult Apply(MultiplierState state, MultiplierContext context)
    {
        if (state.Value < Start || state.Value > Maximum) throw new ArgumentException("Multiplier state is outside the configured range.");
        bool pays = context.Wins.Any(w => w.BasePayoutUnits > 0) && (IncludeInitialGrid || context.GridIndex > 0);
        long next = pays ? state.Value + Math.Min(Increment, Maximum - state.Value) : state.Value;
        return new(state.Value, new(next), []);
    }
}
