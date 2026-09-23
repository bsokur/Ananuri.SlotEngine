using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Fixtures;

internal static class EngineFixtures
{
    internal static string ReadSampleJson(string name)
    {
        using var stream = typeof(EngineFixtures).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Sample resource '{name}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static GameDefinition LoadSample(string name) => GamePackageLoader.Load(ReadSampleJson(name));

    internal static readonly SymbolId A = new(1), B = new(2), C = new(3), W = new(7), S = new(8), M = new(9);

    internal static GameDefinition Fixture(IMultiplierStrategy? paid = null, IMultiplierStrategy? bonus = null,
        int maxSpins = 10, long winLimit = 1000, int retrigger = 2, bool scatterPayout = false,
        bool multiplyScatter = false, bool includeMultiplierSymbol = false)
    {
        var game = new GameDefinition("cascade-test", "1", 2, [A, B, C, W, S, M],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip(includeMultiplierSymbol ? [M, A, B, C, S] : [S, A, B, C])),
            [new Payline(0, [1, 1, 1])], [new PaytableEntry(A, 3, 5), new PaytableEntry(B, 3, 3)], new BetDefinition([100, 200]));
        var refill = new WeightedSymbolRefill(Enumerable.Range(0, 3).Select(_ => new WeightedSymbolTable([new(B, 1), new(C, 1)])));
        return Compose(game, new FallingSymbolsCascade(refill), new TotalStakeWinLimit(winLimit),
            wins: new PaylineWinEvaluator(new OrdinaryWildSubstitution([W], [A, B]), new HighestPayingAward()),
            scatters: new GridScatterPolicy(S, [new(3, 2, scatterPayout ? 2 : 0)], [new(3, retrigger, scatterPayout ? 2 : 0)]),
            features: new FreeSpinsFeature(maxSpins), paidMultiplier: paid,
            bonusMultiplier: bonus ?? new PayingCascadeMultiplier(1, 1, 100, MultiplierPersistence.Bonus),
            multiplyScatterAwards: multiplyScatter);
    }

    internal static BonusState Bonus(GameDefinition game, int remaining = 3, long multiplier = 1,
        long payout = 0, int completed = 0, int? total = null) =>
        new("origin", game.Fingerprint, SlotEngine.EngineVersion, SlotEngine.RulesVersion,
            100, remaining, total ?? remaining + completed, completed, multiplier, payout);

    internal sealed class Tape(params int[] values) : IRandomDrawSource
    {
        private int _index;
        public int Used => _index;
        public int Next(DrawRequest request) => _index < values.Length ? values[_index++]
            : throw new InvalidOperationException($"Test tape exhausted at {request.Purpose}.");
    }

    internal static GameDefinition WildGame(IEnumerable<PaytableEntry>? paytable = null,
        IEnumerable<SymbolId>? priority = null, int rows = 1, IEnumerable<Payline>? lines = null)
    {
        var game = new GameDefinition("wild-test", "1", rows, [A, B, C, W, S],
            Enumerable.Range(0, 5).Select(_ => new ReelStrip([A, B, C, W, S])),
            lines ?? [new Payline(0, [0, 0, 0, 0, 0])],
            paytable ?? [new PaytableEntry(A, 3, 10), new PaytableEntry(B, 5, 4)], new BetDefinition([100]));
        return Compose(game, new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 5)
            .Select(_ => new WeightedSymbolTable([new(C, 1)])))), new TotalStakeWinLimit(100),
            wins: new PaylineWinEvaluator(new OrdinaryWildSubstitution([W], [A, B]), new HighestPayingAward(priority)));
    }

    internal static GameDefinition Compose(GameDefinition game, ICascadePolicy cascades, IWinLimitPolicy winLimit,
        IWinEvaluator? wins = null, IGridGenerator? gridGenerator = null,
        IScatterEvaluator? scatters = null, IFeaturePolicy? features = null,
        IMultiplierStrategy? paidMultiplier = null, IMultiplierStrategy? bonusMultiplier = null,
        IEnumerable<ReelStrip>? freeSpinReels = null, bool multiplyScatterAwards = false,
        bool allowScatterRefills = false) =>
        new(game.GameId, game.MathVersion, game.VisibleRows, game.Symbols, game.Reels, game.Paylines,
            game.Paytable, game.Bets, cascades, winLimit, wins, gridGenerator, scatters, features,
            paidMultiplier, bonusMultiplier, freeSpinReels, multiplyScatterAwards, allowScatterRefills);

    internal static SymbolBoard Board(params SymbolId[] symbols) => new(symbols.Length, 1,
        symbols.Select((symbol, id) => new SymbolInstance(id, symbol)));
    internal sealed class Zeros : IRandomDrawSource { public int Next(DrawRequest request) => 0; }

    internal sealed class BitDraws(int bits) : IRandomDrawSource
    {
        public int Next(DrawRequest request)
        {
            Assert.InRange(request.Ordinal, 0, 5);
            Assert.Equal(2, request.ExclusiveUpperBound);
            return (bits >> (int)request.Ordinal) & 1;
        }
    }
}
