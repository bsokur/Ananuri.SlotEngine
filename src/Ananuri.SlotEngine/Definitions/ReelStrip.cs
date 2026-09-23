using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Definitions;

/// <summary>An immutable ordered strip of symbols. Initial grid generation wraps at the end of the strip.</summary>
public sealed class ReelStrip
{
    public ReelStrip(IEnumerable<SymbolId> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        Symbols = symbols.ToImmutableArray();
        if (Symbols.IsEmpty)
            throw new ArgumentException("A reel cannot be empty.", nameof(symbols));
    }

    /// <summary>Symbols in stop order, including repetitions that determine their initial-grid frequency.</summary>
    public ImmutableArray<SymbolId> Symbols { get; }
    public int Length => Symbols.Length;
    public SymbolId this[int index] => Symbols[index];
}
