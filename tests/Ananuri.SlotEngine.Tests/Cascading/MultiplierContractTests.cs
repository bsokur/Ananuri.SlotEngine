using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Multipliers;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Cascading;

public sealed class MultiplierContractTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("missing-state")]
    [InlineData("missing-collections")]
    [InlineData("null-collection")]
    [InlineData("applied-below-start")]
    [InlineData("applied-above-maximum")]
    [InlineData("next-below-start")]
    [InlineData("next-above-maximum")]
    [InlineData("negative-reel")]
    [InlineData("missing-reel")]
    [InlineData("negative-row")]
    [InlineData("missing-row")]
    [InlineData("instance")]
    [InlineData("symbol")]
    [InlineData("zero-increment")]
    [InlineData("undeclared-symbol")]
    [InlineData("duplicate")]
    public void Evaluate_ShouldRejectMalformedMultiplierOutputBeforeRefilling(string fault)
    {
        var valid = ValidResult();
        var collection = valid.Collections[0];
        var result = fault switch
        {
            "null" => null!,
            "missing-state" => valid with { NextState = null! },
            "missing-collections" => valid with { Collections = default },
            "null-collection" => valid with { Collections = [null!] },
            "applied-below-start" => valid with { AppliedMultiplier = 1 },
            "applied-above-maximum" => valid with { AppliedMultiplier = 6 },
            "next-below-start" => valid with { NextState = new(1) },
            "next-above-maximum" => valid with { NextState = new(6) },
            "negative-reel" => valid with { Collections = [collection with { Position = new(-1, 0) }] },
            "missing-reel" => valid with { Collections = [collection with { Position = new(3, 0) }] },
            "negative-row" => valid with { Collections = [collection with { Position = new(0, -1) }] },
            "missing-row" => valid with { Collections = [collection with { Position = new(0, 2) }] },
            "instance" => valid with { Collections = [collection with { InstanceId = 1 }] },
            "symbol" => valid with { Collections = [collection with { Symbol = A }] },
            "zero-increment" => valid with { Collections = [collection with { Value = 0 }] },
            "undeclared-symbol" => valid with { Collections = [new(1, A, new(0, 1), 1)] },
            "duplicate" => valid with { Collections = [collection, collection] },
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };
        var game = Fixture(paid: new Strategy(_ => result), includeMultiplierSymbol: true);
        var draws = new RecordingDrawSource(new Zeros());

        Assert.Throws<InvalidOperationException>(() => new SlotEngine().Evaluate(game, new("invalid", 100), draws));

        Assert.Equal(game.ReelCount, draws.Snapshot().Length);
    }

    [Fact]
    public void ValidCustomMultiplier_ShouldPreserveAwardsAndRejectRecollectionAfterGravity()
    {
        var game = Fixture(paid: new Strategy(context => ValidResult(context.GridIndex == 0 ? 0 : 1)),
            includeMultiplierSymbol: true);
        var engine = new SlotEngine();
        var draws = new RecordingDrawSource(new Zeros());

        var result = engine.Evaluate(game, new("valid", 100), draws, maxGridEvaluations: 1);

        var step = Assert.Single(result.Steps);
        Assert.Equal(1000, result.TotalPayoutUnits);
        Assert.Equal(2, step.AppliedMultiplier);
        Assert.Equal(3, step.MultiplierAfter);
        Assert.Equal(ValidResult().Collections[0], Assert.Single(step.Collections));
        var checkpoint = Assert.IsType<SpinContinuation>(result.Continuation);
        Assert.Equal(3, checkpoint.Multiplier);
        Assert.Equal(0, Assert.Single(checkpoint.CollectedInstanceIds));
        Assert.Equal(6, draws.Snapshot().Length);

        // Evidence matches the fallen instance, but that instance has already been collected.
        Assert.Equal(0, checkpoint.Grid[0, 1].Id);
        Assert.Equal(M, checkpoint.Grid[0, 1].Symbol);
        Assert.Throws<InvalidOperationException>(() => engine.Resume(game, checkpoint, draws));
        Assert.Equal(6, draws.Snapshot().Length);
    }

    private static MultiplierResult ValidResult(int collectionRow = 0) => new(2, new(3), [new(0, M, new(0, collectionRow), 1)]);

    private sealed class Strategy(Func<MultiplierContext, MultiplierResult> apply) : IMultiplierStrategy
    {
        public string ConfigurationKey => "test-multiplier-output-v1";
        public ImmutableHashSet<SymbolId> CollectionSymbols { get; } = [M];
        public long Start => 2;
        public long Maximum => 5;
        public MultiplierPersistence Persistence => MultiplierPersistence.Spin;
        public void Validate(GameDefinition game) { }
        public MultiplierResult Apply(MultiplierState state, MultiplierContext context) => apply(context);
    }
}
