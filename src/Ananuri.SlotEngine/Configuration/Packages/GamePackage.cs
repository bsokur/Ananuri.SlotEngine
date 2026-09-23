namespace Ananuri.SlotEngine.Configuration.Packages;

/// <summary>Transport configuration for the built-in component profile. Compile through GamePackageLoader to validate and capture immutable game data.</summary>
public sealed class GamePackage
{
    /// <summary>JSON schema version; must match GamePackageLoader.SchemaVersion.</summary>
    public required int SchemaVersion { get; init; }
    /// <summary>Built-in component profile name: slot-engine-v1.</summary>
    public required string Profile { get; init; }
    /// <summary>Built-in cascade selection: none-v1 or falling-symbols-v1.</summary>
    public required string CascadePolicy { get; init; }
    public required string GameId { get; init; }
    public required string MathVersion { get; init; }
    public required int VisibleRows { get; init; }
    public required int[] Symbols { get; init; }
    /// <summary>Paid-spin reel strips in left-to-right reel order, with symbols in stop order.</summary>
    public required int[][] Reels { get; init; }
    /// <summary>Optional free-spin reel strips; null reuses the paid-spin strips.</summary>
    public int[][]? FreeSpinReels { get; init; }
    /// <summary>Line paths, each with one row index per reel.</summary>
    public required PaylinePackage[] Paylines { get; init; }
    /// <summary>Symbol and match-length awards as line-stake multipliers.</summary>
    public required AwardPackage[] Paytable { get; init; }
    /// <summary>Unique positive total stakes in the host's integer accounting unit.</summary>
    public required long[] AllowedStakes { get; init; }
    /// <summary>Paid-spin weighted refill tables, one per reel; required for falling cascades.</summary>
    public WeightPackage[][]? PaidRefillWeights { get; init; }
    /// <summary>Optional free-spin refill tables; null reuses paid-spin tables.</summary>
    public WeightPackage[][]? FreeRefillWeights { get; init; }
    public WildPackage? Wilds { get; init; }
    /// <summary>Optional symbol order for resolving equal-paying line award candidates.</summary>
    public int[]? SymbolPriority { get; init; }
    public ScatterPackage? Scatters { get; init; }
    /// <summary>Optional positive limit on all free spins awarded during one paid round.</summary>
    public int? MaximumAwardedFreeSpins { get; init; }
    /// <summary>Optional paid-spin multiplier configuration; omission selects a constant value of one.</summary>
    public MultiplierPackage? PaidMultiplier { get; init; }
    /// <summary>Optional free-spin multiplier configuration; omission selects a constant value of one.</summary>
    public MultiplierPackage? BonusMultiplier { get; init; }
    /// <summary>Optional positive multiple of the paid stake limiting the entire round's cumulative payout.</summary>
    public long? RoundWinLimitMultiplier { get; init; }
    public bool MultiplyScatterAwards { get; init; }
    public bool AllowScatterRefills { get; init; }
}
