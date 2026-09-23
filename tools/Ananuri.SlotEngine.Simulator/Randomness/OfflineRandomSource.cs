using Ananuri.SlotEngine.Randomness;

namespace Ananuri.SlotEngine.Simulator.Randomness;

internal sealed class OfflineRandomSource(int seed, bool forceFirstStops) : IRandomDrawSource
{
    private readonly Random _random = new(seed);
    public int Next(DrawRequest request) => forceFirstStops && request.EvaluationId == "round-0/paid" && request.Purpose.StartsWith("initial/", StringComparison.Ordinal)
        ? 0 : _random.Next(request.ExclusiveUpperBound);
}
