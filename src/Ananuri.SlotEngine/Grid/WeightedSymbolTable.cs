using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Grid;

/// <summary>An immutable weighted distribution with unique symbols and a total that fits a bounded integer draw.</summary>
public sealed class WeightedSymbolTable
{
    /// <summary>Captures a nonempty distribution of unique symbols with positive weights whose sum fits Int32.</summary>
    public WeightedSymbolTable(IEnumerable<SymbolWeight> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries.ToImmutableArray();
        if (Entries.IsEmpty || Entries.Any(e => e is null || e.Weight <= 0)
            || Entries.Select(e => e.Symbol).Distinct().Count() != Entries.Length)
            throw new ArgumentException("Weights must be positive with unique symbols.");
        long total = Entries.Sum(e => (long)e.Weight);
        if (total > int.MaxValue) throw new ArgumentException("Weight total exceeds the draw bound.");
        TotalWeight = (int)total;
    }
    /// <summary>Entries in draw-interval order; this ordering is part of the configuration.</summary>
    public ImmutableArray<SymbolWeight> Entries { get; }
    /// <summary>Exclusive upper bound used when drawing from this distribution.</summary>
    public int TotalWeight { get; }
    /// <summary>Canonical ordered symbol/weight representation used in component fingerprints.</summary>
    public string ConfigurationKey => string.Join(',', Entries.Select(e => $"{e.Symbol.Value}={e.Weight}"));
    /// <summary>Maps an integer in [0, TotalWeight) to its symbol's contiguous weighted interval.</summary>
    public SymbolId Select(int value)
    {
        if (value < 0 || value >= TotalWeight) throw new ArgumentOutOfRangeException(nameof(value));
        foreach (var entry in Entries)
        {
            if (value < entry.Weight) return entry.Symbol;
            value -= entry.Weight;
        }
        throw new InvalidOperationException("Invalid weight table.");
    }
}
