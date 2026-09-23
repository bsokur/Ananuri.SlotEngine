using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Selects refill symbols independently using one weighted table per reel and spin mode.</summary>
public sealed class WeightedSymbolRefill : IRefillPolicy
{
    /// <summary>Captures one table per reel; omitted free-spin tables reuse the paid-spin tables.</summary>
    public WeightedSymbolRefill(IEnumerable<WeightedSymbolTable> paidTables, IEnumerable<WeightedSymbolTable>? freeTables = null)
    {
        ArgumentNullException.ThrowIfNull(paidTables);
        PaidTables = paidTables.ToImmutableArray();
        FreeTables = freeTables?.ToImmutableArray() ?? PaidTables;
        if (PaidTables.IsEmpty || FreeTables.IsEmpty || PaidTables.Concat(FreeTables).Any(t => t is null))
            throw new ArgumentException("Refill tables must be non-empty and non-null.");
        Symbols = PaidTables.Concat(FreeTables).SelectMany(table => table.Entries)
            .Select(entry => entry.Symbol).ToImmutableHashSet();
    }
    public ImmutableArray<WeightedSymbolTable> PaidTables { get; }
    public ImmutableArray<WeightedSymbolTable> FreeTables { get; }
    public ImmutableHashSet<SymbolId> Symbols { get; }
    public string ConfigurationKey => $"weighted-refill-v1:{string.Join(';', PaidTables.Select(t => t.ConfigurationKey))}|{string.Join(';', FreeTables.Select(t => t.ConfigurationKey))}";
    public void Validate(GameDefinition game)
    {
        if (PaidTables.Length != game.ReelCount || FreeTables.Length != game.ReelCount
            || PaidTables.Concat(FreeTables).SelectMany(t => t.Entries).Any(e => !game.Symbols.Contains(e.Symbol)))
            throw new ArgumentException("Refill tables must match the reels and declared symbols.");
    }
    public SymbolId NextSymbol(GameDefinition game, SpinMode mode, int reel, int row, DrawSequence draws)
    {
        var table = (mode == SpinMode.Free ? FreeTables : PaidTables)[reel];
        return table.Select(draws.Next($"refill/reel/{reel}/row/{row}", table.TotalWeight));
    }
}
