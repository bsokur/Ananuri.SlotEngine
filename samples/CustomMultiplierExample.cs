using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Samples;

/// <summary>Boosts only the initial grid of each spin; later cascade grids use 1x.</summary>
public sealed class FirstGridMultiplier : IMultiplierStrategy
{
    public FirstGridMultiplier(long initialMultiplier)
    {
        if (initialMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialMultiplier), "The initial multiplier must be positive.");
        InitialMultiplier = initialMultiplier;
    }

    public long InitialMultiplier { get; }
    public long Start => 1;
    public long Maximum => InitialMultiplier;
    public MultiplierPersistence Persistence => MultiplierPersistence.Spin;
    public ImmutableHashSet<SymbolId> CollectionSymbols => [];
    public string ConfigurationKey => FormattableString.Invariant($"sample-first-grid-multiplier-v1:{InitialMultiplier}");

    public void Validate(GameDefinition game)
    {
        ArgumentNullException.ThrowIfNull(game);
        // This policy has no game-specific symbol or layout restrictions.
    }

    public MultiplierResult Apply(MultiplierState state, MultiplierContext context)
    {
        if (state.Value < Start || state.Value > Maximum)
            throw new ArgumentException("Multiplier state is outside the configured range.", nameof(state));

        long appliedMultiplier = context.GridIndex == 0 ? InitialMultiplier : 1;
        // GridIndex is persisted by the engine, so no mutable "first grid used" flag is needed.
        return new MultiplierResult(
            AppliedMultiplier: appliedMultiplier,
            NextState: new MultiplierState(Value: 1),
            Collections: []);
    }
}

public static class CustomMultiplierExample
{
    public static GameDefinition CreateGame(long firstGridMultiplier = 3)
    {
        var symbolA = new SymbolId(1);
        var symbolB = new SymbolId(2);
        var symbolC = new SymbolId(3);
        var refillTable = new WeightedSymbolTable([new(symbolB, 1), new(symbolC, 1)]);

        return new GameDefinition(
            gameId: "custom-multiplier-example",
            mathVersion: "1",
            visibleRows: 1,
            symbols: [symbolA, symbolB, symbolC],
            reels: [new ReelStrip([symbolA]), new ReelStrip([symbolA]), new ReelStrip([symbolA])],
            paylines: [new Payline(id: 0, rows: [0, 0, 0])],
            paytable:
            [
                new PaytableEntry(symbol: symbolA, matchCount: 3, multiplier: 2),
                new PaytableEntry(symbol: symbolB, matchCount: 3, multiplier: 1)
            ],
            bets: new BetDefinition(allowedTotalStakeUnits: [100]),
            cascades: new FallingSymbolsCascade(
                new WeightedSymbolRefill(paidTables: [refillTable, refillTable, refillTable])),
            paidMultiplier: new FirstGridMultiplier(initialMultiplier: firstGridMultiplier));
    }

    public static SpinEvaluation Run(long firstGridMultiplier = 3)
    {
        var game = CreateGame(firstGridMultiplier);
        var request = new SpinRequest(EvaluationId: "custom-multiplier-example/0", CalculationStakeUnits: 100);
        return new SlotEngine().Evaluate(game, request, new TeachingDrawSource());
    }

    // Fixed teaching choices: initial AAA, then refill BBB, then refill CCC. Not a random generator.
    private sealed class TeachingDrawSource : IRandomDrawSource
    {
        public int Next(DrawRequest request) => request.Ordinal switch
        {
            >= 0 and < 6 => 0,
            >= 6 and < 9 => 1,
            _ => throw new InvalidOperationException("The example requested an unexpected draw.")
        };
    }
}
