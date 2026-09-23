using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Declares and generates the symbols that can refill removed cells.</summary>
public interface IRefillPolicy : IGameComponent
{
    /// <summary>All symbols that can be generated in either spin mode.</summary>
    ImmutableHashSet<SymbolId> Symbols { get; }
    /// <summary>Returns one declared refill symbol for the requested cell and spin mode, consuming draws as required.</summary>
    SymbolId NextSymbol(GameDefinition game, SpinMode mode, int reel, int row, DrawSequence draws);
}
