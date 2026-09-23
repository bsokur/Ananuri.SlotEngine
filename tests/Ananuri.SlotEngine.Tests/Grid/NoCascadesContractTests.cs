using System.Collections.Immutable;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;
using Xunit;
using static Ananuri.SlotEngine.Tests.Fixtures.EngineFixtures;

namespace Ananuri.SlotEngine.Tests.Grid;

public sealed class NoCascadesContractTests
{
    [Theory]
    [InlineData(SpinMode.Paid)]
    [InlineData(SpinMode.Free)]
    public void EmptyRemovals_ShouldPreserveBoardAndInstanceIdWithoutDrawing(SpinMode mode)
    {
        var game = LoadSample("PaylineGame.json");
        var board = new SymbolBoard(3, 2, Enumerable.Range(0, 6).Select(id => new SymbolInstance(id, A)));
        var draws = new DrawSequence("no-cascades", new Tape(), nextOrdinal: 3);
        ICascadePolicy policy = new NoCascades();
        var removed = policy.SelectRemovals(game, board, []);

        var transition = policy.Apply(game, mode, board, removed, 6, draws);

        Assert.Same(board, transition.Grid);
        Assert.Empty(transition.Removed);
        Assert.Empty(transition.Movements);
        Assert.Empty(transition.Arrivals);
        Assert.Equal(6, transition.NextInstanceId);
        Assert.Equal(3, draws.NextOrdinal);
    }

    [Fact]
    public void NonemptyRemovals_ShouldRejectTheUnsupportedSelection()
    {
        var game = LoadSample("PaylineGame.json");
        var board = new SymbolBoard(3, 2, Enumerable.Range(0, 6).Select(id => new SymbolInstance(id, A)));
        var draws = new DrawSequence("no-cascades", new Tape());

        var error = Assert.Throws<ArgumentException>(() => new NoCascades().Apply(
            game, SpinMode.Paid, board, [new GridPosition(0, 0)], 6, draws));

        Assert.Equal("removed", error.ParamName);
        Assert.Equal(0, draws.NextOrdinal);
    }

    [Fact]
    public void MissingRemovalCollection_ShouldBeRejected()
    {
        var game = LoadSample("PaylineGame.json");

        Assert.Throws<ArgumentException>(() => new NoCascades().Apply(game, SpinMode.Paid,
            Board(A, A, A), default(ImmutableArray<GridPosition>), 3, new DrawSequence("no-cascades", new Tape())));
    }
}
