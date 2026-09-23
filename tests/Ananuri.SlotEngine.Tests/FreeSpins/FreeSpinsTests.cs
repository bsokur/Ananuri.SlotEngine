using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.FreeSpins;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Limits;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.FreeSpins;

public sealed class FreeSpinsTests
{
    private readonly SlotEngine _engine = new();

    [Fact]
    public void FreeSpins_ShouldUseTheirOwnReelsAndRefillTables()
    {
        var baseGame = new GameDefinition("mode-tables", "1", 1, [A, B, C],
            Enumerable.Range(0, 3).Select(_ => new ReelStrip([A])),
            [new Payline(0, [0, 0, 0])], [new PaytableEntry(A, 3, 5), new PaytableEntry(B, 3, 3)],
            new BetDefinition([100]));
        var refill = new WeightedSymbolRefill(
            Enumerable.Range(0, 3).Select(_ => new WeightedSymbolTable([new(C, 1)])),
            Enumerable.Range(0, 3).Select(_ => new WeightedSymbolTable([new(A, 1)])));
        var game = Compose(baseGame, new FallingSymbolsCascade(refill), new TotalStakeWinLimit(10),
            features: new FreeSpinsFeature(10), freeSpinReels: Enumerable.Range(0, 3).Select(_ => new ReelStrip([B])));
        var paid = _engine.Evaluate(game, new("paid", 100), new Zeros());
        var free = _engine.Evaluate(game, new("free", 100, Bonus(game)), new Zeros());
        Assert.Equal(500, paid.TotalPayoutUnits);
        Assert.All(paid.Steps[0].Transition!.Arrivals, a => Assert.Equal(C, a.Instance.Symbol));
        Assert.Equal(B, free.Steps[0].Grid[0, 0].Symbol);
        Assert.All(free.Steps[0].Transition!.Arrivals, a => Assert.Equal(A, a.Instance.Symbol));
        Assert.Equal(new long[] { 300, 500, 200 }, free.Steps.Select(s => s.PayablePayoutUnits));
        Assert.Equal(0, free.ChargedStakeUnits);
        Assert.Equal(CompletionReason.WinLimit, free.CompletionReason);
    }

    [Fact]
    public void PaidSpin_ShouldFinishCascadesBeforeStartingBonus_AndRemoveSharedPositionsOnce()
    {
        var game = Fixture();
        var result = _engine.Evaluate(game, new("paid", 100), new Tape(0, 0, 0, 0, 1, 0));
        Assert.True(result.IsComplete);
        Assert.Equal(500, result.TotalPayoutUnits);
        Assert.Equal(100, result.ChargedStakeUnits);
        Assert.Equal(2, result.Steps.Length);
        Assert.Equal(3, result.Steps[0].Scatter.Count);
        Assert.Equal(0, result.Steps[1].Scatter.Count);
        Assert.Equal(3, result.Steps[0].Transition!.Removed.Length);
        Assert.Equal(2, result.NextBonus!.RemainingSpins);
        Assert.Equal(1, result.NextBonus.Multiplier);
        Assert.Equal(500, result.NextBonus.RoundPayoutUnits);
        Assert.All(result.Steps[0].Transition!.Movements, move =>
        {
            Assert.Equal(0, move.From.Row);
            Assert.Equal(1, move.To.Row);
        });
    }

    [Fact]
    public void Bonus_ShouldPayOldMultiplierThenIncreaseOncePerGrid_AndCarryAcrossFreeSpins()
    {
        var game = Fixture();
        var result = _engine.Evaluate(game, new("free-1", 100, Bonus(game, remaining: 2, payout: 500)),
            new Tape(1, 1, 1, 0, 1, 0, 0, 1, 0));
        Assert.Equal(0, result.ChargedStakeUnits);
        Assert.Equal(100, result.CalculationStakeUnits);
        Assert.Equal(new long[] { 300, 1000, 0 }, result.Steps.Select(s => s.PayablePayoutUnits));
        Assert.Equal(new long[] { 1, 2, 3 }, result.Steps.Select(s => s.AppliedMultiplier));
        Assert.Equal(1300, result.TotalPayoutUnits);
        Assert.Equal(1800, result.RoundPayoutUnits);
        Assert.Equal(3, result.NextBonus!.Multiplier);
        Assert.Equal(1, result.NextBonus.RemainingSpins);
        var loss = _engine.Evaluate(game, new("free-2", 100, result.NextBonus), new Tape(2, 2, 2));
        Assert.Equal(3, Assert.Single(loss.Steps).MultiplierAfter);
        Assert.Equal(0, loss.TotalPayoutUnits);
        Assert.Null(loss.NextBonus);
        Assert.Equal(1800, loss.RoundPayoutUnits);
    }

    [Fact]
    public void LastFreeSpinRetrigger_ShouldExtendBonus_WithoutResettingMultiplier()
    {
        var game = Fixture(maxSpins: 4);
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game, remaining: 1, multiplier: 5, completed: 1, total: 2)),
            new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(2, result.GrantedFreeSpins);
        Assert.Equal(2, result.NextBonus!.RemainingSpins);
        Assert.Equal(6, result.NextBonus.Multiplier);
        Assert.Equal(4, result.NextBonus.TotalSpinsAwarded);
        var last = _engine.Evaluate(game, new("last", 100, result.NextBonus with { CompletedSpins = 3, RemainingSpins = 1 }),
            new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(0, last.GrantedFreeSpins);
        Assert.Null(last.NextBonus);
    }

    [Fact]
    public void Retrigger_ShouldGrantOnlyRemainingLifetimeAllowance()
    {
        var game = Fixture(maxSpins: 4);
        var result = _engine.Evaluate(game, new("free", 100, Bonus(game, remaining: 1, completed: 2, total: 3)),
            new Tape(0, 0, 0, 0, 1, 0));
        Assert.Equal(1, result.GrantedFreeSpins);
        Assert.Equal(1, result.NextBonus!.RemainingSpins);
        Assert.Equal(4, result.NextBonus.TotalSpinsAwarded);
    }

    [Fact]
    public void InvalidState_ShouldFailBeforeAllocatingRandomness()
    {
        var game = Fixture();
        var tape = new Tape();
        var bad = Bonus(game) with { GameFingerprint = "wrong" };
        Assert.Throws<ArgumentException>(() => _engine.Evaluate(game, new("free", 100, bad), tape));
        Assert.Throws<ArgumentException>(() => _engine.Evaluate(game, new("free", 200, Bonus(game)), tape));
        Assert.Throws<ArgumentException>(() => _engine.Evaluate(game, new("free", 100, Bonus(game) with { RemainingSpins = -1 }), tape));
        Assert.Equal(0, tape.Used);
    }
}
