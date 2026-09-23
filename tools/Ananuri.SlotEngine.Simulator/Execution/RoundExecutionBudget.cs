namespace Ananuri.SlotEngine.Simulator.Execution;

internal sealed class RoundExecutionBudget
{
    internal const int DefaultMaxGrids = 10_000;
    internal const int DefaultMaxSpins = 1_000;
    private readonly int _maxGrids;
    private readonly int _maxSpins;
    internal CancellationToken CancellationToken { get; }
    internal int UsedGrids { get; private set; }
    internal int UsedSpins { get; private set; }

    internal RoundExecutionBudget(int maxGrids = DefaultMaxGrids, int maxSpins = DefaultMaxSpins,
        CancellationToken cancellationToken = default)
    {
        if (maxGrids <= 0) throw new ArgumentOutOfRangeException(nameof(maxGrids));
        if (maxSpins <= 0) throw new ArgumentOutOfRangeException(nameof(maxSpins));
        _maxGrids = maxGrids;
        _maxSpins = maxSpins;
        CancellationToken = cancellationToken;
    }

    internal void BeginSpin()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (UsedSpins == _maxSpins)
            throw new InvalidOperationException($"Round exceeded its {_maxSpins}-spin budget; the round is unfinished. Increase --max-spins-per-round to continue a longer run.");
        UsedSpins++;
    }

    internal int NextGridBatch()
    {
        CancellationToken.ThrowIfCancellationRequested();
        int remaining = _maxGrids - UsedGrids;
        if (remaining == 0)
            throw new InvalidOperationException($"Round exceeded its {_maxGrids}-grid budget; the round is unfinished. Increase --max-grids-per-round to continue a longer run.");
        return Math.Min(remaining, 256);
    }

    internal void ObserveGrids(int count)
    {
        if (count < 0 || count > _maxGrids - UsedGrids)
            throw new InvalidOperationException("Engine returned more grids than the remaining round budget.");
        UsedGrids += count;
        CancellationToken.ThrowIfCancellationRequested();
    }
}
