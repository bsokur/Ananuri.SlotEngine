using System.Collections.Frozen;
using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Configuration.Validation;
using Ananuri.SlotEngine.Definitions.Validation;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;

namespace Ananuri.SlotEngine.Definitions;

/// <summary>
/// Immutable game data and policies used by <see cref="SlotEngine"/>.
/// Omitted policies select ordinary paylines, no cascades or free spins, 1x multipliers, and no payout cap.
/// </summary>
public sealed class GameDefinition
{
    private readonly FrozenDictionary<(SymbolId Symbol, int Count), PaytableEntry> _awards;

    /// <summary>Captures game data, supplies defaults for omitted policies, validates all components, and calculates the canonical fingerprint.</summary>
    public GameDefinition(
        string gameId,
        string mathVersion,
        int visibleRows,
        IEnumerable<SymbolId> symbols,
        IEnumerable<ReelStrip> reels,
        IEnumerable<Payline> paylines,
        IEnumerable<PaytableEntry> paytable,
        BetDefinition bets,
        ICascadePolicy? cascades = null,
        IWinLimitPolicy? winLimit = null,
        IWinEvaluator? wins = null,
        IGridGenerator? gridGenerator = null,
        IScatterEvaluator? scatters = null,
        IFeaturePolicy? features = null,
        IMultiplierStrategy? paidMultiplier = null,
        IMultiplierStrategy? bonusMultiplier = null,
        IEnumerable<ReelStrip>? freeSpinReels = null,
        bool multiplyScatterAwards = false,
        bool allowScatterRefills = false)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(reels);
        ArgumentNullException.ThrowIfNull(paylines);
        ArgumentNullException.ThrowIfNull(paytable);
        ArgumentNullException.ThrowIfNull(bets);
        GameId = gameId;
        MathVersion = mathVersion;
        VisibleRows = visibleRows;
        Symbols = symbols.ToImmutableArray();
        Reels = reels.ToImmutableArray();
        Paylines = paylines.ToImmutableArray();
        Paytable = paytable.ToImmutableArray();
        Bets = bets;
        GameDefinitionValidator.Validate(this);
        _awards = Paytable.ToFrozenDictionary(entry => (entry.Symbol, entry.MatchCount));

        Cascades = cascades ?? new NoCascades();
        WinLimit = winLimit ?? new NoWinLimit();
        Wins = wins ?? new PaylineWinEvaluator(new OrdinaryWildSubstitution([], []), new HighestPayingAward());
        GridGenerator = gridGenerator ?? new ReelStripGridGenerator();
        Scatters = scatters ?? new NoScatters();
        Features = features ?? new NoFreeSpins();
        PaidMultiplier = paidMultiplier ?? new ConstantMultiplier();
        BonusMultiplier = bonusMultiplier ?? new ConstantMultiplier();
        FreeSpinReels = freeSpinReels?.ToImmutableArray() ?? Reels;
        MultiplyScatterAwards = multiplyScatterAwards;
        AllowScatterRefills = allowScatterRefills;
        PayingSymbols = (Wins.GetPayingSymbols(this)
            ?? throw new ArgumentException("Win evaluator must declare its paying symbols."))
            .OrderBy(symbol => symbol.Value).ToImmutableArray();
        GameConfigurationValidator.Validate(this);
        Fingerprint = GameDefinitionFingerprint.Compute(this);
    }

    public string GameId { get; }
    /// <summary>Host-defined version identifying this game's mathematical rules.</summary>
    public string MathVersion { get; }
    public int VisibleRows { get; }
    public int ReelCount => Reels.Length;
    public ImmutableArray<SymbolId> Symbols { get; }
    /// <summary>Ordered reel strips used to generate paid-spin initial grids.</summary>
    public ImmutableArray<ReelStrip> Reels { get; }
    /// <summary>Configured line paths; built-in paylines divide the total stake equally among these lines.</summary>
    public ImmutableArray<Payline> Paylines { get; }
    /// <summary>Configured line-stake awards for symbol and match-length pairs.</summary>
    public ImmutableArray<PaytableEntry> Paytable { get; }
    public BetDefinition Bets { get; }
    /// <summary>Ordered strips used for free-spin initial grids; defaults to the paid strips.</summary>
    public ImmutableArray<ReelStrip> FreeSpinReels { get; }
    /// <summary>Symbols declared as capable of ordinary awards by the win evaluator.</summary>
    public ImmutableArray<SymbolId> PayingSymbols { get; }
    public IGridGenerator GridGenerator { get; }
    /// <summary>Policy evaluating base ordinary awards and declaring their bounds; defaults to payline evaluation.</summary>
    public IWinEvaluator Wins { get; }
    public ICascadePolicy Cascades { get; }
    public IScatterEvaluator Scatters { get; }
    public IFeaturePolicy Features { get; }
    public IMultiplierStrategy PaidMultiplier { get; }
    public IMultiplierStrategy BonusMultiplier { get; }
    /// <summary>Policy limiting the cumulative payout of a paid spin and all its awarded free spins.</summary>
    public IWinLimitPolicy WinLimit { get; }
    public bool MultiplyScatterAwards { get; }
    public bool AllowScatterRefills { get; }
    /// <summary>Canonical hash of game data, component configuration, and engine rules used to bind persisted state.</summary>
    public string Fingerprint { get; }

    // Order is part of the canonical fingerprint and must remain stable.
    internal IGameComponent[] Components =>
        [GridGenerator, Wins, Cascades, Scatters, Features, PaidMultiplier, BonusMultiplier, WinLimit];

    internal PaytableEntry? FindAward(SymbolId symbol, int consecutiveCount)
    {
        // Longest qualifying match; missing lengths fall back to shorter entries.
        for (int count = consecutiveCount; count > 0; count--)
            if (_awards.TryGetValue((symbol, count), out var award)) return award;
        return null;
    }
}
