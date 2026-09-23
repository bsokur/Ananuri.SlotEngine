using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class PolicyContractTests
{
    [Theory]
    [InlineData("scatter-collection")]
    [InlineData("scatter-paying")]
    [InlineData("collection-paying")]
    [InlineData("scatter-substitution")]
    [InlineData("collection-substitution")]
    public void CustomPolicies_ShouldRejectConflictingSymbolRoles(string conflict)
    {
        var wins = new TestWins(_ => 0,
            paying: conflict switch { "scatter-paying" => [S], "collection-paying" => [M], _ => [A] },
            substitution: conflict switch { "scatter-substitution" => [S], "collection-substitution" => [M], _ => [] });
        var collection = conflict == "scatter-collection" ? S : M;

        Assert.Throws<ArgumentException>(() => Create(wins: wins,
            scatters: new TestScatters(), paid: new TestMultiplier([collection])));
    }

    [Theory]
    [InlineData("paying")]
    [InlineData("substitution")]
    [InlineData("scatter")]
    [InlineData("collection")]
    [InlineData("refill")]
    public void CustomPolicies_ShouldRejectUndeclaredSymbols(string role)
    {
        var unknown = new SymbolId(99);
        var wins = new TestWins(_ => 0, paying: role == "paying" ? [unknown] : [A],
            substitution: role == "substitution" ? [unknown] : []);
        var scatters = new TestScatters(symbols: role == "scatter" ? [unknown] : [S]);
        var paid = new TestMultiplier(role == "collection" ? [unknown] : []);
        var cascades = new CascadeProxy(new FallingSymbolsCascade(new TestRefill(role == "refill" ? unknown : B)));

        Assert.Throws<ArgumentException>(() => Create(wins: wins, scatters: scatters, paid: paid, cascades: cascades));
    }

    [Fact]
    public void CustomSubstitution_ShouldExposeItsTargetsToCompatibilityValidation()
    {
        var substitution = new SubstitutionProxy(new OrdinaryWildSubstitution([W], [S]));
        var wins = new PaylineWinEvaluator(substitution, new HighestPayingAward());

        Assert.Throws<ArgumentException>(() => Create(wins: wins, scatters: new TestScatters()));
    }

    [Theory]
    [InlineData("actual")]
    [InlineData("target")]
    public void CustomSubstitution_ShouldNotSubstituteUndeclaredSymbols(string undeclaredRole)
    {
        var wins = new PaylineWinEvaluator(new UndeclaredSubstitution(), new HighestPayingAward());
        // A wins the tie, so B's invalid substitution must be detected before award selection.
        var game = Create(wins: wins, scatters: new TestScatters(),
            reelSymbol: undeclaredRole == "actual" ? S : W,
            paytable: undeclaredRole == "target" ? [new(A, 1, 1), new(B, 1, 1)] : [new(A, 1, 1)]);

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("undeclared-substitution", 100), new Zeros()));
    }

    [Theory]
    [InlineData("actual")]
    [InlineData("target")]
    public void CustomEvaluator_ShouldNotReportUndeclaredWildSymbols(string undeclaredRole)
    {
        var target = undeclaredRole == "actual" ? A : B;
        var game = Create(wins: new TestWins(_ => 100, payouts: [100], paying: [target],
                substitution: [W, A], substitutedTarget: target),
            scatters: new TestScatters(), reelSymbol: undeclaredRole == "actual" ? S : W);

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("undeclared-wild-award", 100), new Zeros()));
    }

    [Fact]
    public void CustomScatterTriggers_ShouldRequireFeatureSupport()
    {
        Assert.Throws<ArgumentException>(() => Create(scatters: new TestScatters(canAwardFreeSpins: true),
            features: new FeatureProxy(new NoFreeSpins())));

        var game = Create(reelSymbol: S,
            scatters: new TestScatters(canAwardFreeSpins: true, freeSpins: 1),
            features: new FeatureProxy(new FreeSpinsFeature(5)));
        var result = new SlotEngine().Evaluate(game, new("custom-feature", 100), new Zeros());

        Assert.NotNull(result.NextBonus);
        Assert.Equal(1, result.NextBonus.RemainingSpins);
    }

    [Fact]
    public void CustomCascadeAndRefill_ShouldRequireScatterRefillOptIn()
    {
        var cascades = new CascadeProxy(new FallingSymbolsCascade(new TestRefill(S)));
        var wins = new TestWins(_ => 10, payouts: [10]);
        Assert.Throws<ArgumentException>(() => Create(wins: wins, scatters: new TestScatters(), cascades: cascades));

        var game = Create(wins: wins, scatters: new TestScatters(), cascades: cascades, allowScatterRefills: true);
        var result = new SlotEngine().Evaluate(game, new("custom-refill", 100), new Zeros());

        Assert.Equal(10, result.TotalPayoutUnits);
        Assert.Equal(2, result.Steps.Length);
        Assert.Equal(S, result.Steps[1].Grid[0, 0].Symbol);
    }

    [Theory]
    [InlineData("negative")]
    [InlineData("overflow")]
    [InlineData("multiplied-overflow")]
    [InlineData("combined-overflow")]
    public void CustomEvaluatorBound_ShouldBeCheckedAtEveryConfiguredStake(string invalid)
    {
        BigInteger lowerStakeBound = invalid switch
        {
            "negative" => -1,
            "overflow" => (BigInteger)long.MaxValue + 1,
            _ => long.MaxValue
        };
        var wins = new TestWins(stake => stake == 1 ? lowerStakeBound : 0);
        var scatter = new TestScatters(maximum: invalid == "combined-overflow" ? 1 : 0);
        var multiplier = new ConstantMultiplier(invalid == "multiplied-overflow" ? 2 : 1);

        Assert.Throws<ArgumentException>(() => Create(wins: wins, stakes: [1, 2], scatters: scatter, paid: multiplier));
    }

    [Fact]
    public void SafeCustomEvaluator_CanUseNonMonotonicStakeBounds()
    {
        var game = Create(wins: new TestWins(stake => stake == 1 ? 100 : 50, payouts: [25]), stakes: [1, 2]);
        var engine = new SlotEngine();

        Assert.Equal(25, engine.Evaluate(game, new("lower-stake", 1), new Zeros()).TotalPayoutUnits);
        Assert.Equal(25, engine.Evaluate(game, new("higher-stake", 2), new Zeros()).TotalPayoutUnits);
    }

    [Fact]
    public void CustomEvaluatorBounds_ShouldIgnoreUnusedPaytableAmounts()
    {
        var game = Create(wins: new TestWins(_ => 7, payouts: [7]),
            paytable: [new PaytableEntry(A, 1, long.MaxValue)]);

        Assert.Equal(7, new SlotEngine().Evaluate(game, new("unused-paytable", 100), new Zeros()).TotalPayoutUnits);
    }

    [Fact]
    public void CustomEvaluator_CanRunWithoutPaylinesOrPaytable()
    {
        var game = Create(wins: new TestWins(_ => 7, payouts: [7]), paylines: [], paytable: []);

        Assert.Equal(7, new SlotEngine().Evaluate(game, new("no-paylines", 100), new Zeros()).TotalPayoutUnits);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultPaylineEvaluator_ShouldRequireItsOwnData(bool missingPaylines)
    {
        Assert.Throws<ArgumentException>(() => Create(paylines: missingPaylines ? [] : null,
            paytable: missingPaylines ? null : []));
    }

    [Fact]
    public void StakeDivisibility_ShouldBeSpecificToThePaylineEvaluator()
    {
        Payline[] paylines = [new(0, [0]), new(1, [1])];
        Assert.Throws<ArgumentException>(() => Create(rows: 2, paylines: paylines, stakes: [3]));

        var game = Create(rows: 2, paylines: paylines, stakes: [3], wins: new TestWins(_ => 3, payouts: [3]));
        Assert.Equal(3, new SlotEngine().Evaluate(game, new("custom-stake", 3), new Zeros()).TotalPayoutUnits);
    }

    [Theory]
    [InlineData("wins", false)]
    [InlineData("wins", true)]
    [InlineData("scatters", false)]
    [InlineData("scatters", true)]
    public void CustomPolicyOutputs_ShouldRespectTheirDeclaredPayoutBounds(string source, bool exceedsBound)
    {
        long payout = exceedsBound ? 101 : 100;
        var wins = new TestWins(_ => source == "wins" ? 100 : 0, payouts: source == "wins" ? [payout] : []);
        var scatters = new TestScatters(maximum: source == "scatters" ? 1 : 0,
            payout: source == "scatters" ? payout : 0);
        var game = Create(wins: wins, scatters: scatters, reelSymbol: source == "scatters" ? S : A);

        SpinEvaluation Evaluate() => new SlotEngine().Evaluate(game, new("bound-contract", 100), new Zeros());
        if (exceedsBound) Assert.Throws<InvalidOperationException>(Evaluate);
        else Assert.Equal(100, Evaluate().TotalPayoutUnits);
    }

    [Fact]
    public void WinBound_ShouldApplyToTheTotalOfAllAwards()
    {
        var game = Create(wins: new TestWins(_ => 100, payouts: [60, 60]));

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("award-total", 100), new Zeros()));
    }

    [Theory]
    [InlineData("wins")]
    [InlineData("scatters")]
    public void PayoutCaps_ShouldNotBypassDeclaredPayoutBounds(string source)
    {
        var wins = new TestWins(_ => source == "wins" ? 100 : 0, payouts: source == "wins" ? [101] : []);
        var scatters = new TestScatters(maximum: source == "scatters" ? 1 : 0,
            payout: source == "scatters" ? 101 : 0);
        var game = Create(wins: wins, scatters: scatters, reelSymbol: source == "scatters" ? S : A,
            winLimit: new TotalStakeWinLimit(1));

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("capped-contract", 100), new Zeros()));
    }

    [Theory]
    [InlineData("collection")]
    [InlineData("refill")]
    public void CustomOutputs_ShouldRespectDeclaredCollectionAndRefillSymbols(string source)
    {
        var game = Create(wins: new TestWins(_ => 10, payouts: [10]),
            paid: new TestMultiplier([M], collect: source == "collection"),
            cascades: source == "refill" ? new CascadeProxy(new FallingSymbolsCascade(new TestRefill(B, S))) : null);

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("symbol-contract", 100), new Zeros()));
    }

    [Fact]
    public void Scatter_ShouldNotTriggerAnUndeclaredCapabilityEvenWhenFeatureSupportsIt()
    {
        var game = Create(reelSymbol: S, scatters: new TestScatters(canAwardFreeSpins: false, freeSpins: 1),
            features: new FeatureProxy(new FreeSpinsFeature(5)));

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("unexpected-trigger", 100), new Zeros()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledCustomFeature_ShouldNotReturnFreeSpinStateOrGrants(bool returnsBonus)
    {
        var game = Create(features: new UndeclaredFeature(returnsBonus));

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("unexpected-feature", 100), new Zeros()));
    }

    private static GameDefinition Create(IWinEvaluator? wins = null, IScatterEvaluator? scatters = null,
        IMultiplierStrategy? paid = null, ICascadePolicy? cascades = null, IFeaturePolicy? features = null,
        IEnumerable<long>? stakes = null, IEnumerable<Payline>? paylines = null,
        IEnumerable<PaytableEntry>? paytable = null, SymbolId? reelSymbol = null, int rows = 1,
        bool allowScatterRefills = false, IWinLimitPolicy? winLimit = null) =>
        new("policy-contract", "1", rows, [A, B, W, S, M],
            [new ReelStrip(Enumerable.Repeat(reelSymbol ?? A, rows))],
            paylines ?? [new Payline(0, [0])], paytable ?? [new PaytableEntry(A, 1, 1)],
            new BetDefinition(stakes ?? [100]), wins: wins, scatters: scatters, paidMultiplier: paid,
            cascades: cascades, features: features, allowScatterRefills: allowScatterRefills, winLimit: winLimit);

    private sealed class TestWins(Func<long, BigInteger> bound, IEnumerable<long>? payouts = null,
        IEnumerable<SymbolId>? paying = null, IEnumerable<SymbolId>? substitution = null,
        SymbolId? substitutedTarget = null) : IWinEvaluator
    {
        private readonly ImmutableArray<long> _payouts = (payouts ?? []).ToImmutableArray();
        private readonly ImmutableHashSet<SymbolId> _paying = (paying ?? [A]).ToImmutableHashSet();
        public ImmutableHashSet<SymbolId> SubstitutionSymbols { get; } = (substitution ?? []).ToImmutableHashSet();
        public string ConfigurationKey => "test-wins-v1";
        public void Validate(GameDefinition game) { }
        public ImmutableHashSet<SymbolId> GetPayingSymbols(GameDefinition game) => _paying;
        public BigInteger MaximumBasePayout(GameDefinition game, long calculationStake) => bound(calculationStake);
        public ImmutableArray<WinAward> Evaluate(GameDefinition game, SymbolBoard grid, long calculationStake) =>
            _paying.Contains(substitutedTarget ?? grid[0, 0].Symbol)
                ? _payouts.Select(amount => new WinAward(null, substitutedTarget ?? grid[0, 0].Symbol, 1, 1, amount,
                    [new(0, 0)], substitutedTarget.HasValue ? [new(0, 0)] : [])).ToImmutableArray()
                : [];
    }

    private sealed class TestScatters(long maximum = 0, long payout = 0, bool canAwardFreeSpins = false,
        int freeSpins = 0, IEnumerable<SymbolId>? symbols = null) : IScatterEvaluator
    {
        public ImmutableHashSet<SymbolId> Symbols { get; } = (symbols ?? [S]).ToImmutableHashSet();
        public long MaximumAwardMultiplier => maximum;
        public bool CanAwardFreeSpins => canAwardFreeSpins;
        public string ConfigurationKey => "test-scatters-v1";
        public void Validate(GameDefinition game) { }
        public ScatterResult Evaluate(GameDefinition game, ScatterContext context) =>
            Symbols.Contains(context.Grid[0, 0].Symbol)
                ? new(1, freeSpins, payout, [new(0, 0)], [context.Grid[0, 0].Id])
                : ScatterResult.None;
    }

    private sealed class TestMultiplier(IEnumerable<SymbolId> symbols, bool collect = false) : IMultiplierStrategy
    {
        public ImmutableHashSet<SymbolId> CollectionSymbols { get; } = symbols.ToImmutableHashSet();
        public long Start => 1;
        public long Maximum => 1;
        public MultiplierPersistence Persistence => MultiplierPersistence.Spin;
        public string ConfigurationKey => "test-multiplier-v1";
        public void Validate(GameDefinition game) { }
        public MultiplierResult Apply(MultiplierState state, MultiplierContext context) =>
            new(1, new(1), collect
                ? [new(context.Grid[0, 0].Id, context.Grid[0, 0].Symbol, new(0, 0), 1)]
                : []);
    }

    private sealed class TestRefill(SymbolId symbol, SymbolId? emittedSymbol = null) : IRefillPolicy
    {
        public ImmutableHashSet<SymbolId> Symbols { get; } = [symbol];
        public string ConfigurationKey => $"test-refill-v1:{symbol.Value}";
        public void Validate(GameDefinition game) { }
        public SymbolId NextSymbol(GameDefinition game, SpinMode mode, int reel, int row, DrawSequence draws) => emittedSymbol ?? symbol;
    }

    private sealed class CascadeProxy(ICascadePolicy inner) : ICascadePolicy
    {
        public ImmutableHashSet<SymbolId> RefillSymbols => inner.RefillSymbols;
        public string ConfigurationKey => $"test-cascade-wrapper-v1[{inner.ConfigurationKey}]";
        public void Validate(GameDefinition game) => inner.Validate(game);
        public ImmutableArray<GridPosition> SelectRemovals(GameDefinition game, SymbolBoard grid, ImmutableArray<WinAward> wins) =>
            inner.SelectRemovals(game, grid, wins);
        public CascadeTransition Apply(GameDefinition game, SpinMode mode, SymbolBoard grid,
            ImmutableArray<GridPosition> removed, long nextInstanceId, DrawSequence draws) =>
            inner.Apply(game, mode, grid, removed, nextInstanceId, draws);
    }

    private sealed class SubstitutionProxy(ISymbolSubstitutionPolicy inner) : ISymbolSubstitutionPolicy
    {
        public ImmutableHashSet<SymbolId> InvolvedSymbols => inner.InvolvedSymbols;
        public string ConfigurationKey => $"test-substitution-wrapper-v1[{inner.ConfigurationKey}]";
        public void Validate(GameDefinition game) => inner.Validate(game);
        public bool CanSubstitute(SymbolId actual, SymbolId target) => inner.CanSubstitute(actual, target);
    }

    private sealed class UndeclaredSubstitution : ISymbolSubstitutionPolicy
    {
        public ImmutableHashSet<SymbolId> InvolvedSymbols => [W, A];
        public string ConfigurationKey => "test-undeclared-substitution-v1";
        public void Validate(GameDefinition game) { }
        public bool CanSubstitute(SymbolId actual, SymbolId target) => true;
    }

    private sealed class FeatureProxy(IFeaturePolicy inner) : IFeaturePolicy
    {
        public bool SupportsFreeSpins => inner.SupportsFreeSpins;
        public string ConfigurationKey => $"test-feature-wrapper-v1[{inner.ConfigurationKey}]";
        public void Validate(GameDefinition game) => inner.Validate(game);
        public void ValidateState(GameDefinition game, BonusState state) => inner.ValidateState(game, state);
        public FeatureTransition Complete(GameDefinition game, SpinRequest request,
            long pendingFreeSpins, long multiplier, long roundPayout, bool winLimitReached) =>
            inner.Complete(game, request, pendingFreeSpins, multiplier, roundPayout, winLimitReached);
    }

    private sealed class UndeclaredFeature(bool returnsBonus) : IFeaturePolicy
    {
        public bool SupportsFreeSpins => false;
        public string ConfigurationKey => $"test-undeclared-feature-v1:{returnsBonus}";
        public void Validate(GameDefinition game) { }
        public void ValidateState(GameDefinition game, BonusState state) { }
        public FeatureTransition Complete(GameDefinition game, SpinRequest request,
            long pendingFreeSpins, long multiplier, long roundPayout, bool winLimitReached) =>
            returnsBonus ? new(Bonus(game, remaining: 1, payout: roundPayout, total: 1), 0) : new(null, 1);
    }
}
