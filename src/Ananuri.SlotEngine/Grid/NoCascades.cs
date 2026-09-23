using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Ends evaluation after the initial grid by selecting no cells for removal.</summary>
public sealed class NoCascades : ICascadePolicy
{
    public ImmutableHashSet<SymbolId> RefillSymbols => [];
    public string ConfigurationKey => "no-cascades-v1";
    public void Validate(GameDefinition game) { }
    public ImmutableArray<GridPosition> SelectRemovals(GameDefinition game, SymbolBoard grid, ImmutableArray<WinAward> wins) => [];
    public CascadeTransition Apply(GameDefinition game, SpinMode mode, SymbolBoard grid,
        ImmutableArray<GridPosition> removed, long nextInstanceId, DrawSequence draws)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (removed.IsDefault)
            throw new ArgumentException("A removal collection is required.", nameof(removed));
        if (!removed.IsEmpty)
            throw new ArgumentException("The no-cascades policy selects no removals; an empty collection is required.", nameof(removed));
        return new(grid, [], [], [], nextInstanceId);
    }
}
