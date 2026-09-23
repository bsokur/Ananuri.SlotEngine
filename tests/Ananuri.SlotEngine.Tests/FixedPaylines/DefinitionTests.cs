using Ananuri.SlotEngine.Definitions;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.FixedPaylines;

public sealed class DefinitionTests
{
    private static GameDefinition Create(
        IEnumerable<SymbolId>? symbols = null,
        IEnumerable<ReelStrip>? reels = null,
        IEnumerable<Payline>? paylines = null,
        IEnumerable<PaytableEntry>? payouts = null,
        BetDefinition? bets = null,
        int rows = 2) => new("validation-test", "1", rows,
            symbols ?? new[] { A },
            reels ?? Enumerable.Range(0, 3).Select(_ => new ReelStrip(new[] { A })),
            paylines ?? new[] { new Payline(0, new[] { 0, 0, 0 }) },
            payouts ?? new[] { new PaytableEntry(A, 3, 5) },
            bets ?? new BetDefinition(new long[] { 100 }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenReelUsesUndeclaredSymbol() =>
        Assert.Throws<ArgumentException>(() => Create(symbols: new[] { B }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenPaylineRowIsOutsideGrid() =>
        Assert.Throws<ArgumentException>(() => Create(paylines: new[] { new Payline(0, new[] { 0, 2, 0 }) }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenPaylineLengthIsWrong() =>
        Assert.Throws<ArgumentException>(() => Create(paylines: new[] { new Payline(0, new[] { 0, 0 }) }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenPaylinePathsAreDuplicated() =>
        Assert.Throws<ArgumentException>(() => Create(paylines: new[]
        {
            new Payline(0, new[] { 0, 0, 0 }), new Payline(1, new[] { 0, 0, 0 })
        }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenAwardsAreDuplicated() =>
        Assert.Throws<ArgumentException>(() => Create(payouts: new[]
        {
            new PaytableEntry(A, 3, 5), new PaytableEntry(A, 3, 6)
        }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenLongerMatchPaysLess() =>
        Assert.Throws<ArgumentException>(() => Create(payouts: new[]
        {
            new PaytableEntry(A, 2, 10), new PaytableEntry(A, 3, 5)
        }));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenStakeCannotDivideAcrossLines() =>
        Assert.Throws<ArgumentException>(() => Create(
            paylines: new[] { new Payline(0, new[] { 0, 0, 0 }), new Payline(1, new[] { 1, 1, 1 }) },
            bets: new BetDefinition(new long[] { 101 })));

    [Fact]
    public void Constructor_ShouldRejectGame_WhenPayoutBoundOverflows() =>
        Assert.Throws<ArgumentException>(() => Create(bets: new BetDefinition(new[] { long.MaxValue })));

    [Fact]
    public void Constructor_ShouldCopySymbols_WhenOriginalArrayChanges()
    {
        var symbols = new[] { A };
        var strip = new ReelStrip(symbols);
        symbols[0] = B;
        Assert.Equal(A, strip[0]);
    }
}
