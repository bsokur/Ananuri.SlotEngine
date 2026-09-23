using System.Collections.Frozen;
using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Increases a capped multiplier once per eligible symbol instance, retaining collection identity when symbols fall.</summary>
public sealed class CollectedSymbolMultiplier : IMultiplierStrategy
{
    /// <summary>Captures positive collection increments and multiplier bounds. ApplyBeforeAward controls whether this grid uses newly collected increments.</summary>
    public CollectedSymbolMultiplier(long start, long maximum, MultiplierPersistence persistence,
        IEnumerable<KeyValuePair<SymbolId, long>> values, bool applyBeforeAward = true, bool requiresWin = false)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (start <= 0 || maximum < start || !Enum.IsDefined(persistence))
            throw new ArgumentException("Invalid collected-symbol multiplier configuration.");
        Values = values.ToFrozenDictionary();
        if (Values.Count == 0 || Values.Values.Any(v => v <= 0))
            throw new ArgumentException("Collection increments must be positive and non-empty.");
        Start = start; Maximum = maximum; Persistence = persistence;
        ApplyBeforeAward = applyBeforeAward; RequiresWin = requiresWin;
        CollectionSymbols = Values.Keys.ToImmutableHashSet();
    }
    public long Start { get; }
    public long Maximum { get; }
    public MultiplierPersistence Persistence { get; }
    /// <summary>Immutable symbol-to-increment mapping; increments are capped when applied.</summary>
    public FrozenDictionary<SymbolId, long> Values { get; }
    public ImmutableHashSet<SymbolId> CollectionSymbols { get; }
    public bool ApplyBeforeAward { get; }
    /// <summary>Whether collection requires a positive base ordinary award on the current grid; scatter awards do not qualify.</summary>
    public bool RequiresWin { get; }
    public string ConfigurationKey => $"collected-symbol-multiplier-v1:{Start}:{Maximum}:{Persistence}:{ApplyBeforeAward}:{RequiresWin}:{string.Join(',', Values.OrderBy(p => p.Key.Value).Select(p => $"{p.Key.Value}={p.Value}"))}";
    public void Validate(GameDefinition game)
    {
        if (Values.Keys.Any(s => !game.Symbols.Contains(s)))
            throw new ArgumentException("Collection symbols must be declared.");
    }
    public MultiplierResult Apply(MultiplierState state, MultiplierContext context)
    {
        if (state.Value < Start || state.Value > Maximum) throw new ArgumentException("Multiplier state is outside the configured range.");
        if (RequiresWin && !context.Wins.Any(w => w.BasePayoutUnits > 0)) return new(state.Value, state, []);
        long value = state.Value;
        var collections = ImmutableArray.CreateBuilder<SymbolCollection>();
        for (int reel = 0; reel < context.Grid.ReelCount; reel++)
            for (int row = 0; row < context.Grid.RowCount; row++)
            {
                var cell = context.Grid[reel, row];
                if (context.CollectedInstanceIds.Contains(cell.Id) || !Values.TryGetValue(cell.Symbol, out long increment)) continue;
                value += Math.Min(increment, Maximum - value);
                collections.Add(new(cell.Id, cell.Symbol, new GridPosition(reel, row), increment));
            }
        return new(ApplyBeforeAward ? value : state.Value, new(value), collections.ToImmutable());
    }
}
