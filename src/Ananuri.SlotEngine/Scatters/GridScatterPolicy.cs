using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Scatters;

/// <summary>Highest satisfied threshold per eligible batch. Moving symbol instances are never counted twice.</summary>
public sealed class GridScatterPolicy : IScatterEvaluator
{
    /// <summary>Captures ordered monotonic count thresholds. Omitted free-spin thresholds award nothing during free spins.</summary>
    public GridScatterPolicy(SymbolId symbol, IEnumerable<ScatterThreshold> paidThresholds,
        IEnumerable<ScatterThreshold>? freeThresholds = null, bool atMostOnePerReel = false,
        ScatterTiming timing = ScatterTiming.InitialGrid, bool allowMultipleFeatureAwards = false)
    {
        ArgumentNullException.ThrowIfNull(paidThresholds);
        Symbol = symbol;
        Symbols = [symbol];
        PaidThresholds = Capture(paidThresholds);
        FreeThresholds = Capture(freeThresholds ?? []);
        AtMostOnePerReel = atMostOnePerReel;
        if (!Enum.IsDefined(timing)) throw new ArgumentOutOfRangeException(nameof(timing));
        Timing = timing;
        AllowMultipleFeatureAwards = allowMultipleFeatureAwards;
    }
    public SymbolId Symbol { get; }
    public ImmutableHashSet<SymbolId> Symbols { get; }
    public bool CanAwardFreeSpins => PaidThresholds.Concat(FreeThresholds).Any(t => t.FreeSpins > 0);
    /// <summary>Count thresholds for paid spins, ordered by minimum count.</summary>
    public ImmutableArray<ScatterThreshold> PaidThresholds { get; }
    /// <summary>Count thresholds for free spins, ordered by minimum count.</summary>
    public ImmutableArray<ScatterThreshold> FreeThresholds { get; }
    public bool AtMostOnePerReel { get; }
    public ScatterTiming Timing { get; }
    public bool AllowMultipleFeatureAwards { get; }
    public long MaximumAwardMultiplier => PaidThresholds.Concat(FreeThresholds).Select(t => t.TotalStakeMultiplier).DefaultIfEmpty().Max();
    public string ConfigurationKey => $"grid-scatters-v1:{Symbol.Value}:{AtMostOnePerReel}:{Timing}:{AllowMultipleFeatureAwards}:{Key(PaidThresholds)}|{Key(FreeThresholds)}";
    private static string Key(ImmutableArray<ScatterThreshold> entries) => string.Join(';', entries.Select(t => $"{t.MinimumCount},{t.FreeSpins},{t.TotalStakeMultiplier}"));
    private static ImmutableArray<ScatterThreshold> Capture(IEnumerable<ScatterThreshold> entries)
    {
        var result = entries.ToImmutableArray();
        if (result.Any(t => t is null || t.MinimumCount <= 0 || t.FreeSpins < 0 || t.TotalStakeMultiplier < 0)
            || result.Select(t => t.MinimumCount).Distinct().Count() != result.Length)
            throw new ArgumentException("Invalid or duplicate scatter thresholds.");
        result = result.OrderBy(t => t.MinimumCount).ToImmutableArray();
        for (int i = 1; i < result.Length; i++)
            if (result[i].FreeSpins < result[i - 1].FreeSpins || result[i].TotalStakeMultiplier < result[i - 1].TotalStakeMultiplier)
                throw new ArgumentException("Scatter awards must not decrease at higher thresholds.");
        return result;
    }
    public void Validate(GameDefinition game)
    {
        int maxCount = AtMostOnePerReel ? game.ReelCount : checked(game.ReelCount * game.VisibleRows);
        if (!game.Symbols.Contains(Symbol)
            || PaidThresholds.Concat(FreeThresholds).Any(t => t.MinimumCount > maxCount))
            throw new ArgumentException("Scatter must be a declared symbol with reachable count thresholds.");
    }
    public ScatterResult Evaluate(GameDefinition game, ScatterContext context)
    {
        if (Timing == ScatterTiming.InitialGrid && context.GridIndex != 0) return ScatterResult.None;
        var grid = context.Grid;
        var positions = ImmutableArray.CreateBuilder<GridPosition>();
        var instances = ImmutableArray.CreateBuilder<long>();
        var countedReels = new HashSet<int>();
        for (int reel = 0; reel < grid.ReelCount; reel++)
            for (int row = 0; row < grid.RowCount; row++)
            {
                var cell = grid[reel, row];
                if (cell.Symbol != Symbol || context.CountedInstanceIds.Contains(cell.Id)) continue;
                instances.Add(cell.Id);
                if (!AtMostOnePerReel || countedReels.Add(reel)) positions.Add(new(reel, row));
            }
        var thresholds = context.Mode == SpinMode.Paid ? PaidThresholds : FreeThresholds;
        var award = thresholds.LastOrDefault(t => t.MinimumCount <= positions.Count);
        int freeSpins = context.FeatureAlreadyTriggered && !AllowMultipleFeatureAwards ? 0 : award?.FreeSpins ?? 0;
        return new(positions.Count, freeSpins,
            checked(context.CalculationStakeUnits * (award?.TotalStakeMultiplier ?? 0)), positions.ToImmutable(), instances.ToImmutable());
    }
}
