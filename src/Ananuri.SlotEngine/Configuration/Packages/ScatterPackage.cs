using Ananuri.SlotEngine.Scatters;

namespace Ananuri.SlotEngine.Configuration.Packages;

public sealed class ScatterPackage
{
    public required int Symbol { get; init; }
    public required ScatterThreshold[] PaidThresholds { get; init; }
    /// <summary>Optional scatter thresholds for free spins; null awards nothing in free spins.</summary>
    public ScatterThreshold[]? FreeThresholds { get; init; }
    public bool AtMostOnePerReel { get; init; }
    public ScatterTiming Timing { get; init; } = ScatterTiming.InitialGrid;
    public bool AllowMultipleFeatureAwards { get; init; }
}
