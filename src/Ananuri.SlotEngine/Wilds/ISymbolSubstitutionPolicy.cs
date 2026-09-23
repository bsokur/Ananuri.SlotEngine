using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Wilds;

/// <summary>Declares allowed substitutions from actual grid symbols to candidate paying symbols.</summary>
public interface ISymbolSubstitutionPolicy : IGameComponent
{
    /// <summary>All symbols on either side of a supported substitution.</summary>
    ImmutableHashSet<SymbolId> InvolvedSymbols { get; }
    /// <summary>Determines whether an actual symbol can represent the target paying symbol.</summary>
    /// <param name="actual">Symbol present on the board.</param>
    /// <param name="target">Paying symbol used to construct a candidate award.</param>
    /// <returns>True only for supported substitutions whose two symbols are declared in InvolvedSymbols.</returns>
    bool CanSubstitute(SymbolId actual, SymbolId target);
}
