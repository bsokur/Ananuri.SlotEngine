using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Wilds;

public sealed class WildsTests
{
    [Fact]
    public void WildCandidates_ShouldChooseHighestPayout_NotLongestMatch()
    {
        var game = WildGame();
        var award = Assert.Single(game.Wins.Evaluate(game, Board(W, W, W, B, B), 100));
        Assert.Equal(A, award.Symbol);
        Assert.Equal(3, award.MatchCount);
        Assert.Equal(1000, award.BasePayoutUnits);
        Assert.Equal(3, award.WildPositions.Length);
        Assert.Equal(3, award.Positions.Length);
    }

    [Fact]
    public void AllWildLine_ShouldConsiderOwnAward_AndApplyDeterministicTieBreak()
    {
        var game = WildGame([new(A, 3, 5), new(B, 5, 5), new(W, 5, 5)], [W, B, A]);
        var award = Assert.Single(game.Wins.Evaluate(game, Board(W, W, W, W, W), 100));
        Assert.Equal(W, award.Symbol);
        Assert.Equal(5, award.MatchCount);
        Assert.Empty(award.WildPositions);
        var ordinary = WildGame([new(A, 3, 5), new(B, 5, 5)]);
        Assert.Equal(B, Assert.Single(ordinary.Wins.Evaluate(ordinary, Board(W, W, W, W, W), 100)).Symbol);
    }

    [Fact]
    public void Substitution_ShouldNotMatchExcludedSymbolsOrRestartAfterMismatch()
    {
        var game = WildGame([new(A, 3, 5), new(C, 3, 7)]);
        Assert.Empty(game.Wins.Evaluate(game, Board(W, C, C, C, C), 100));
        Assert.Empty(game.Wins.Evaluate(game, Board(B, A, A, A, A), 100));
        Assert.Equal(A, Assert.Single(game.Wins.Evaluate(game, Board(W, A, A, B, C), 100)).Symbol);
    }
}
