using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Applies the same positive multiplier to every evaluated grid.</summary>
public sealed class ConstantMultiplier : IMultiplierStrategy
{
    public ConstantMultiplier(long value = 1)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        Start = value;
    }
    public long Start { get; }
    public ImmutableHashSet<SymbolId> CollectionSymbols => [];
    public long Maximum => Start;
    public MultiplierPersistence Persistence => MultiplierPersistence.Spin;
    public string ConfigurationKey => $"constant-multiplier-v1:{Start}";
    public void Validate(GameDefinition game) { }
    public MultiplierResult Apply(MultiplierState state, MultiplierContext context) => new(Start, new(Start), []);
}
