using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Scatters;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Wilds;
using Ananuri.SlotEngine.Wins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class OutputContractTests
{
    [Fact]
    public void Selection_ShouldRejectInventedAwardOnLosingBoard()
    {
        var game = SelectionGame(new Selector(_ => new(0, A, 1, 1, 50, [new(0, 0)], [])), B);

        var error = Assert.Throws<InvalidOperationException>(() => Evaluate(game));

        Assert.Contains("candidate", error.Message);
    }

    [Theory]
    [InlineData("payline")]
    [InlineData("symbol")]
    [InlineData("count")]
    [InlineData("multiplier")]
    [InlineData("payout")]
    [InlineData("positions")]
    [InlineData("wilds")]
    public void Selection_ShouldRejectAlteredCandidateEvenWithinPayoutBound(string field)
    {
        var game = SelectionGame(new Selector(candidates => field switch
        {
            "payline" => candidates[0] with { PaylineId = 9 },
            "symbol" => candidates[0] with { Symbol = B },
            "count" => candidates[0] with { MatchCount = 2 },
            "multiplier" => candidates[0] with { PaytableMultiplier = 2 },
            "payout" => candidates[0] with { BasePayoutUnits = 50 },
            "positions" => candidates[0] with { Positions = [] },
            "wilds" => candidates[0] with { WildPositions = [new(0, 0)] },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        }));

        Assert.Throws<InvalidOperationException>(() => Evaluate(game));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Selection_CanReturnUnchangedCandidateOrNull(bool skip)
    {
        var game = SelectionGame(new Selector(candidates => skip ? null : candidates[0] with { }));

        Assert.Equal(skip ? 0 : 100, Evaluate(game).TotalPayoutUnits);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("negative-grant")]
    [InlineData("invented-grant")]
    [InlineData("discarded-bonus")]
    [InlineData("fingerprint")]
    [InlineData("engine")]
    [InlineData("rules")]
    [InlineData("stake")]
    [InlineData("payout")]
    [InlineData("origin")]
    [InlineData("total")]
    [InlineData("completed")]
    [InlineData("remaining")]
    [InlineData("multiplier")]
    public void CompletedPaidSpin_ShouldRejectMalformedFeatureTransition(string fault)
    {
        var game = FeatureGame(new Feature(transition => Corrupt(transition, fault)));

        Assert.Throws<InvalidOperationException>(() => Evaluate(game));
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("payout")]
    [InlineData("discarded-bonus")]
    [InlineData("completed")]
    [InlineData("total")]
    [InlineData("remaining")]
    [InlineData("multiplier")]
    public void CompletedFreeSpin_ShouldPreserveRoundAndConsumeExactlyOneSpin(string fault)
    {
        var game = FeatureGame(new Feature(transition => Corrupt(transition, fault)));
        var previous = Bonus(game, remaining: 2, completed: 1, total: 3, multiplier: 3, payout: 200);

        Assert.Throws<InvalidOperationException>(() => Evaluate(game, previous));
    }

    [Fact]
    public void CompletedSpin_ShouldApplyPolicySpecificStateValidationBeforePublishing()
    {
        var policy = new Feature(transition => transition, rejectState: true);
        var game = FeatureGame(policy);

        var error = Assert.Throws<InvalidOperationException>(() => Evaluate(game));

        Assert.Single(policy.ValidatedStates);
        Assert.IsType<ArgumentException>(error.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cap_ShouldRejectAnyOutgoingGrantOrBonus(bool bonus)
    {
        var policy = new Feature(_ => bonus ? new(new("paid", "invalid", SlotEngine.EngineVersion,
            SlotEngine.RulesVersion, 100, 1, 1, 0, 1, 100), 0) : new(null, 1));
        var game = FeatureGame(policy, cap: true);

        Assert.Throws<InvalidOperationException>(() => Evaluate(game));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuiltInFeature_ShouldDiscardPendingGrantsAndRemainingBonusAtCap(bool free)
    {
        var game = FeatureGame(new FreeSpinsFeature(10), cap: true);
        var previous = free ? Bonus(game, remaining: 2) : null;

        var result = Evaluate(game, previous);

        Assert.Equal(CompletionReason.WinLimit, result.CompletionReason);
        Assert.Equal(0, result.GrantedFreeSpins);
        Assert.Null(result.NextBonus);
    }

    [Theory]
    [InlineData(MultiplierPersistence.Spin, 1)]
    [InlineData(MultiplierPersistence.Bonus, 4)]
    public void ValidCustomFeature_ShouldPreserveExpectedMultiplierAndRoundState(
        MultiplierPersistence persistence, long expectedMultiplier)
    {
        var policy = new Feature(transition => transition);
        var game = FeatureGame(policy, persistence: persistence);
        var previous = Bonus(game, remaining: 2, completed: 1, total: 3,
            multiplier: persistence == MultiplierPersistence.Bonus ? 3 : 1, payout: 200);

        var result = Evaluate(game, previous);

        Assert.Equal(1, result.NextBonus!.RemainingSpins);
        Assert.Equal(3, result.NextBonus.TotalSpinsAwarded);
        Assert.Equal(2, result.NextBonus.CompletedSpins);
        Assert.Equal("origin", result.NextBonus.OriginatingRoundId);
        Assert.Equal(expectedMultiplier, result.NextBonus.Multiplier);
        Assert.Equal(200 + result.TotalPayoutUnits, result.NextBonus.RoundPayoutUnits);
        Assert.Contains(result.NextBonus, policy.ValidatedStates);
    }

    [Fact]
    public void CustomFeature_CanLimitRequestedGrantsAndFinishTheLastFreeSpin()
    {
        var feature = new Feature(transition => transition, maximumSpins: 1);
        var game = FeatureGame(feature);

        var paid = Evaluate(game);
        var free = Evaluate(game, paid.NextBonus);

        Assert.Equal(2, paid.Steps[0].Scatter.RequestedFreeSpins);
        Assert.Equal(1, paid.GrantedFreeSpins);
        Assert.Equal(1, paid.NextBonus!.RemainingSpins);
        Assert.Equal(0, free.GrantedFreeSpins);
        Assert.Null(free.NextBonus);
    }

    [Fact]
    public void NewBonus_ShouldStartAtBonusMultiplierInsteadOfPaidMultiplier()
    {
        var game = FeatureGame(new Feature(transition => transition), paidMultiplier: new ConstantMultiplier(5));

        var result = Evaluate(game);

        Assert.Equal(500, result.TotalPayoutUnits);
        Assert.Equal(1, result.NextBonus!.Multiplier);
    }

    [Fact]
    public void Feature_ShouldNotReturnAnExhaustedBonusAfterLastFreeSpin()
    {
        var game = FeatureGame(new Feature(transition => transition.Bonus is null
            ? new(new("origin", "invalid", SlotEngine.EngineVersion, SlotEngine.RulesVersion,
                100, 0, 1, 1, 1, 100), 0) : transition));

        Assert.Throws<InvalidOperationException>(() => Evaluate(game, Bonus(game, remaining: 1)));
    }

    private static FeatureTransition Corrupt(FeatureTransition transition, string fault) => fault switch
    {
        "null" => null!,
        "negative-grant" => transition with { GrantedFreeSpins = -5 },
        "invented-grant" => transition with { GrantedFreeSpins = 3 },
        "discarded-bonus" => transition with { Bonus = null },
        "fingerprint" => transition with { Bonus = transition.Bonus! with { GameFingerprint = "another-game" } },
        "engine" => transition with { Bonus = transition.Bonus! with { EngineVersion = "other" } },
        "rules" => transition with { Bonus = transition.Bonus! with { RulesVersion = "other" } },
        "stake" => transition with { Bonus = transition.Bonus! with { CalculationStakeUnits = 200 } },
        "payout" => transition with { Bonus = transition.Bonus! with { RoundPayoutUnits = 0 } },
        "origin" => transition with { Bonus = transition.Bonus! with { OriginatingRoundId = "another-round" } },
        "total" => transition with { Bonus = transition.Bonus! with { TotalSpinsAwarded = 4 } },
        "completed" => transition with { Bonus = transition.Bonus! with { CompletedSpins = 5 } },
        "remaining" => transition with { Bonus = transition.Bonus! with { RemainingSpins = -1 } },
        "multiplier" => transition with { Bonus = transition.Bonus! with { Multiplier = 7 } },
        _ => throw new ArgumentOutOfRangeException(nameof(fault))
    };

    private static GameDefinition SelectionGame(IAwardSelectionPolicy selection, SymbolId? symbol = null) =>
        new("selection-output", "1", 1, [A, B], [new ReelStrip([symbol ?? A])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 1)], new BetDefinition([100]),
            wins: new PaylineWinEvaluator(new OrdinaryWildSubstitution([], []), selection));

    private static GameDefinition FeatureGame(IFeaturePolicy feature, bool cap = false,
        MultiplierPersistence persistence = MultiplierPersistence.Bonus, IMultiplierStrategy? paidMultiplier = null) =>
        new("feature-output", "1", 2, [A, B, S], [new ReelStrip([A, S])],
            [new Payline(0, [0])], [new PaytableEntry(A, 1, 1)], new BetDefinition([100, 200]),
            winLimit: cap ? new TotalStakeWinLimit(1) : new NoWinLimit(),
            scatters: new GridScatterPolicy(S, [new(1, 2, 0)]), features: feature,
            paidMultiplier: paidMultiplier,
            bonusMultiplier: new PayingCascadeMultiplier(1, 1, 10, persistence),
            freeSpinReels: [new ReelStrip([A, B])]);

    private static SpinEvaluation Evaluate(GameDefinition game, BonusState? bonus = null) =>
        new SlotEngine().Evaluate(game, new(bonus is null ? "paid" : "free", 100, bonus), new Zeros());

    private sealed class Selector(Func<ImmutableArray<WinAward>, WinAward?> select) : IAwardSelectionPolicy
    {
        public string ConfigurationKey => "test-selection-output-v1";
        public void Validate(GameDefinition game) { }
        public WinAward? Select(ImmutableArray<WinAward> candidates) => select(candidates);
    }

    private sealed class Feature(Func<FeatureTransition, FeatureTransition> transform,
        bool rejectState = false, int maximumSpins = 10) : IFeaturePolicy
    {
        private readonly FreeSpinsFeature _inner = new(maximumSpins);
        public bool SupportsFreeSpins => true;
        public string ConfigurationKey => "test-feature-output-v1";
        public List<BonusState> ValidatedStates { get; } = [];
        public void Validate(GameDefinition game) { }
        public void ValidateState(GameDefinition game, BonusState state)
        {
            ValidatedStates.Add(state);
            if (rejectState) throw new ArgumentException("Policy-specific bonus restriction.");
        }
        public FeatureTransition Complete(GameDefinition game, SpinRequest request,
            long pendingFreeSpins, long multiplier, long roundPayout, bool winLimitReached) =>
            transform(_inner.Complete(game, request, pendingFreeSpins, multiplier, roundPayout, winLimitReached));
    }
}
