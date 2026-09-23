using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Wins;

/// <summary>Chooses the largest base payout, breaking ties by match count, symbol priority, then symbol identifier.</summary>
public sealed class HighestPayingAward : IAwardSelectionPolicy
{
    /// <param name="symbolPriority">Symbols in preferred order; omitted symbols follow listed symbols.</param>
    public HighestPayingAward(IEnumerable<SymbolId>? symbolPriority = null)
    {
        SymbolPriority = (symbolPriority ?? []).ToImmutableArray();
        if (SymbolPriority.Distinct().Count() != SymbolPriority.Length)
            throw new ArgumentException("Symbol priority contains duplicates.", nameof(symbolPriority));
    }
    /// <summary>Immutable symbol order used after payout and match-count ties.</summary>
    public ImmutableArray<SymbolId> SymbolPriority { get; }
    public string ConfigurationKey => $"highest-payout-v1:{string.Join(',', SymbolPriority.Select(s => s.Value))}";
    public void Validate(GameDefinition game)
    {
        if (SymbolPriority.Any(s => !game.Symbols.Contains(s))) throw new ArgumentException("Unknown priority symbol.");
    }
    public WinAward? Select(ImmutableArray<WinAward> candidates) => candidates
        .OrderByDescending(c => c.BasePayoutUnits).ThenByDescending(c => c.MatchCount)
        .ThenBy(c => Priority(c.Symbol)).ThenBy(c => c.Symbol.Value).FirstOrDefault();
    private int Priority(SymbolId symbol)
    {
        int index = SymbolPriority.IndexOf(symbol);
        return index < 0 ? int.MaxValue : index;
    }
}
