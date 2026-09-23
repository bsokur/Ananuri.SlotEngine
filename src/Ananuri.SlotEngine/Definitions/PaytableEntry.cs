namespace Ananuri.SlotEngine.Definitions;

/// <summary>A positive line-stake multiplier for a symbol and qualifying consecutive match length.</summary>
public sealed record PaytableEntry
{
    public PaytableEntry(SymbolId symbol, int matchCount, long multiplier)
    {
        if (matchCount <= 0) throw new ArgumentOutOfRangeException(nameof(matchCount));
        if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier));
        Symbol = symbol;
        MatchCount = matchCount;
        Multiplier = multiplier;
    }

    public SymbolId Symbol { get; }
    /// <summary>Qualifying consecutive match length from the leftmost reel.</summary>
    public int MatchCount { get; }
    /// <summary>Positive factor applied to one line's share of the total stake.</summary>
    public long Multiplier { get; }
}
