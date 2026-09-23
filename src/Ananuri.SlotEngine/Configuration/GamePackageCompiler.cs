using Ananuri.SlotEngine.Configuration.Packages;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Configuration;

internal static class GamePackageCompiler
{
    public static GameDefinition Compile(GamePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (package.SchemaVersion != GamePackageLoader.SchemaVersion || package.Profile != SlotEngine.RulesVersion)
            throw new ArgumentException("Unsupported schema version or rule profile.");
        if (package.Symbols is null || package.Reels is null || package.Paylines is null || package.Paytable is null
            || package.AllowedStakes is null
            || package.VisibleRows is <= 0 or > 128 || package.Reels.Length is <= 0 or > 128
            || package.Symbols.Length is <= 0 or > 4096 || package.Paylines.Length > 100_000)
            throw new ArgumentException("Missing collections or unsupported package dimensions.");
        var wild = package.Wilds;
        var substitution = new OrdinaryWildSubstitution(
            (wild?.Symbols ?? []).Select(s => new SymbolId(s)),
            (wild?.SubstitutesFor ?? []).Select(s => new SymbolId(s)));
        IScatterEvaluator scatter = package.Scatters is null ? new NoScatters()
            : new GridScatterPolicy(new(package.Scatters.Symbol), package.Scatters.PaidThresholds,
                package.Scatters.FreeThresholds, package.Scatters.AtMostOnePerReel,
                package.Scatters.Timing, package.Scatters.AllowMultipleFeatureAwards);
        IFeaturePolicy features = package.MaximumAwardedFreeSpins is int maximum ? new FreeSpinsFeature(maximum) : new NoFreeSpins();
        return new GameDefinition(package.GameId, package.MathVersion, package.VisibleRows,
            package.Symbols.Select(s => new SymbolId(s)), Reels(package.Reels),
            package.Paylines.Select(p => p is null ? throw new ArgumentException("Null payline.") : new Payline(p.Id, p.Rows)),
            package.Paytable.Select(p => p is null ? throw new ArgumentException("Null award.") : new PaytableEntry(new(p.Symbol), p.MatchCount, p.Multiplier)),
            new BetDefinition(package.AllowedStakes),
            cascades: Cascades(package),
            winLimit: package.RoundWinLimitMultiplier is long limit ? new TotalStakeWinLimit(limit) : new NoWinLimit(),
            wins: new PaylineWinEvaluator(substitution, new HighestPayingAward((package.SymbolPriority ?? []).Select(s => new SymbolId(s)))),
            scatters: scatter, features: features,
            paidMultiplier: Multiplier(package.PaidMultiplier), bonusMultiplier: Multiplier(package.BonusMultiplier),
            freeSpinReels: package.FreeSpinReels is null ? null : Reels(package.FreeSpinReels),
            multiplyScatterAwards: package.MultiplyScatterAwards, allowScatterRefills: package.AllowScatterRefills);
    }

    private static ICascadePolicy Cascades(GamePackage package)
    {
        switch (package.CascadePolicy)
        {
            case "none-v1":
                if (package.PaidRefillWeights is not null || package.FreeRefillWeights is not null || package.AllowScatterRefills)
                    throw new ArgumentException("The no-cascades policy does not accept refill settings.");
                return new NoCascades();
            case "falling-symbols-v1":
                return new FallingSymbolsCascade(new WeightedSymbolRefill(
                    Tables(package.PaidRefillWeights ?? throw new ArgumentException("Falling symbols require paid refill weights.")),
                    package.FreeRefillWeights is null ? null : Tables(package.FreeRefillWeights)));
            default:
                throw new ArgumentException($"Unsupported cascade policy '{package.CascadePolicy}'.");
        }
    }

    private static IEnumerable<ReelStrip> Reels(int[][] reels) => reels.Select(r =>
        new ReelStrip((r ?? throw new ArgumentException("Null reel.")).Select(s => new SymbolId(s))));
    private static IEnumerable<WeightedSymbolTable> Tables(WeightPackage[][] tables) => tables.Select(t =>
        new WeightedSymbolTable((t ?? throw new ArgumentException("Null weight table.")).Select(w =>
            w is null ? throw new ArgumentException("Null weight.") : new SymbolWeight(new(w.Symbol), w.Weight))));

    private static IMultiplierStrategy Multiplier(MultiplierPackage? settings)
    {
        if (settings is null) return new ConstantMultiplier();
        switch (settings.Strategy)
        {
            case "constant-v1":
                if (settings.Increment is not null || settings.Maximum is not null || settings.Persistence is not null
                    || settings.CollectionValues is not null || settings.IncludeInitialGrid is not null
                    || settings.ApplyBeforeAward is not null || settings.RequiresWin is not null)
                    throw new ArgumentException("Constant multiplier accepts only start.");
                return new ConstantMultiplier(settings.Start);
            case "paying-cascade-v1":
                if (settings.CollectionValues is not null || settings.ApplyBeforeAward is not null || settings.RequiresWin is not null)
                    throw new ArgumentException("Paying-cascade multiplier cannot contain collection settings.");
                return new PayingCascadeMultiplier(settings.Start,
                    settings.Increment ?? throw new ArgumentException("Increment is required."),
                    settings.Maximum ?? throw new ArgumentException("Maximum is required."),
                    settings.Persistence ?? throw new ArgumentException("Persistence is required."), settings.IncludeInitialGrid ?? true);
            case "collected-symbol-v1":
                if (settings.Increment is not null || settings.IncludeInitialGrid is not null)
                    throw new ArgumentException("Collection multiplier cannot contain cascade settings.");
                return new CollectedSymbolMultiplier(settings.Start,
                    settings.Maximum ?? throw new ArgumentException("Maximum is required."),
                    settings.Persistence ?? throw new ArgumentException("Persistence is required."),
                    (settings.CollectionValues ?? throw new ArgumentException("Collection values are required."))
                        .Select(p => p is null ? throw new ArgumentException("Null collection value.")
                            : new KeyValuePair<SymbolId, long>(new(p.Symbol), p.Value)),
                    settings.ApplyBeforeAward ?? true, settings.RequiresWin ?? false);
            default: throw new ArgumentException($"Unsupported multiplier strategy '{settings.Strategy}'.");
        }
    }
}
