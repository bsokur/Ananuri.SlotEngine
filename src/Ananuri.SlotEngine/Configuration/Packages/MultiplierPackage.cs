using Ananuri.SlotEngine.Multipliers;

namespace Ananuri.SlotEngine.Configuration.Packages;

/// <summary>Transport settings for one built-in multiplier strategy; only fields supported by the selected strategy are accepted.</summary>
public sealed class MultiplierPackage
{
    /// <summary>Built-in multiplier strategy: constant-v1, paying-cascade-v1, or collected-symbol-v1.</summary>
    public required string Strategy { get; init; }
    public required long Start { get; init; }
    public long? Increment { get; init; }
    /// <summary>Inclusive multiplier cap for a strategy that increases its value.</summary>
    public long? Maximum { get; init; }
    public MultiplierPersistence? Persistence { get; init; }
    public bool? IncludeInitialGrid { get; init; }
    /// <summary>Positive multiplier increments indexed by their collection symbol.</summary>
    public CollectionPackage[]? CollectionValues { get; init; }
    public bool? ApplyBeforeAward { get; init; }
    public bool? RequiresWin { get; init; }
}
