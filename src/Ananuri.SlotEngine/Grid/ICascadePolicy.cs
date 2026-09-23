using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Selects removals and transforms a winning board into the next grid while preserving instance identity.</summary>
public interface ICascadePolicy : IGameComponent
{
    /// <summary>All symbols that can arrive through this cascade in either spin mode.</summary>
    ImmutableHashSet<SymbolId> RefillSymbols { get; }
    /// <summary>Selects the cells this policy can remove. An empty result ends the spin's cascade sequence.</summary>
    ImmutableArray<GridPosition> SelectRemovals(GameDefinition game, SymbolBoard grid, ImmutableArray<WinAward> wins);
    /// <summary>
    /// Applies removals selected by this policy for the supplied board. The engine calls this method only
    /// for a nonempty selection from <see cref="SelectRemovals"/>; policies may reject other removal sets.
    /// An empty selection, when supplied directly, leaves the board and next instance ID unchanged.
    /// Removes the selected instances, preserves all survivors' IDs and symbols, and fills the board
    /// with fresh contiguous IDs starting at nextInstanceId. Reports every movement and arrival exactly once.
    /// Movement and arrival evidence may be returned in any order; removals retain the supplied order.
    /// </summary>
    CascadeTransition Apply(GameDefinition game, SpinMode mode, SymbolBoard grid,
        ImmutableArray<GridPosition> removed, long nextInstanceId, DrawSequence draws);
}
