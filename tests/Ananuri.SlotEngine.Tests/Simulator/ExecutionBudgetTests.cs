using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Simulator.Execution;
using Ananuri.SlotEngine.Spins;
using Ananuri.SlotEngine.Tests.Fixtures;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Simulator;

public sealed class ExecutionBudgetTests
{
    [Fact]
    public void UncappedEndlessCascadeStopsAtTheOverallBudgetAcrossContinuationBatches()
    {
        var budget = new RoundExecutionBudget(300, cancellationToken: TestContext.Current.CancellationToken);
        var source = new CountingZeros();
        var exception = Assert.Throws<InvalidOperationException>(() => SpinExecutor.EvaluateComplete(
            new SlotEngine(), EndlessGame(), new SpinRequest("round", 100), source, budget));

        Assert.Contains("300-grid budget", exception.Message);
        Assert.Contains("unfinished", exception.Message);
        Assert.Equal(300, budget.UsedGrids);
        Assert.Equal(1, budget.UsedSpins);
        Assert.Equal(3 + 300 * 3, source.Count);
    }

    [Fact]
    public void CancellationInterruptsAnEndlessCascadeInsideABatch()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var source = new CancelAfterDraws(cancellation, 20);
        var budget = new RoundExecutionBudget(cancellationToken: cancellation.Token);

        Assert.Throws<OperationCanceledException>(() => SpinExecutor.EvaluateComplete(
            new SlotEngine(), EndlessGame(), new SpinRequest("round", 100), source, budget));
        Assert.Equal(20, source.Count);
    }

    [Fact]
    public void BudgetIncludesPaidAndFreeSpinsOfTheSameRound()
    {
        var game = EngineFixtures.LoadSample("FirstGame.json");
        var source = new EngineFixtures.Zeros();
        var budget = new RoundExecutionBudget(4, 2, TestContext.Current.CancellationToken);
        var engine = new SlotEngine();
        var paid = SpinExecutor.EvaluateComplete(engine, game, new SpinRequest("round/paid", 100), source, budget);
        var free = SpinExecutor.EvaluateComplete(engine, game, new SpinRequest("round/free-0", 100, paid.NextBonus), source, budget);

        Assert.NotNull(free.NextBonus);
        Assert.Equal(4, budget.UsedGrids);
        Assert.Equal(2, budget.UsedSpins);
        var exception = Assert.Throws<InvalidOperationException>(() => SpinExecutor.EvaluateComplete(engine, game,
            new SpinRequest("round/free-1", 100, free.NextBonus), source, budget));
        Assert.Contains("2-spin budget", exception.Message);
    }

    [Fact]
    public void ACompletedSpinMayUseExactlyTheRemainingGridBudget()
    {
        var game = EngineFixtures.LoadSample("PaylineGame.json");
        var budget = new RoundExecutionBudget(1, 1, TestContext.Current.CancellationToken);
        var result = SpinExecutor.EvaluateComplete(new SlotEngine(), game, new SpinRequest("round", 100),
            new EngineFixtures.Zeros(), budget);

        Assert.True(result.IsComplete);
        Assert.Single(result.Steps);
        Assert.Equal(600, result.TotalPayoutUnits);
        Assert.Equal(1, budget.UsedGrids);
    }

    [Fact]
    public void CancellationBeforeTheSpinDoesNotConsumeAnyDraws()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        var source = new CountingZeros();
        var budget = new RoundExecutionBudget(cancellationToken: cancellation.Token);
        Assert.Throws<OperationCanceledException>(() => SpinExecutor.EvaluateComplete(
            new SlotEngine(), EndlessGame(), new SpinRequest("round", 100), source, budget));
        Assert.Equal(0, source.Count);
        Assert.Equal(0, budget.UsedSpins);
    }

    private static GameDefinition EndlessGame()
    {
        var symbol = new SymbolId(1);
        return new GameDefinition("endless-cascade-test", "1", 1, [symbol],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip([symbol])),
            [new Payline(0, [0, 0, 0])], [new PaytableEntry(symbol, 3, 1)], new BetDefinition([100]),
            cascades: new FallingSymbolsCascade(new WeightedSymbolRefill(Enumerable.Range(0, 3)
                .Select(_ => new WeightedSymbolTable([new(symbol, 1)])))));
    }

    private sealed class CountingZeros : IRandomDrawSource
    {
        internal int Count { get; private set; }
        public int Next(DrawRequest request)
        {
            Count++;
            return 0;
        }
    }

    private sealed class CancelAfterDraws(CancellationTokenSource cancellation, int stopAfter) : IRandomDrawSource
    {
        internal int Count { get; private set; }
        public int Next(DrawRequest request)
        {
            if (++Count == stopAfter) cancellation.Cancel();
            return 0;
        }
    }
}
