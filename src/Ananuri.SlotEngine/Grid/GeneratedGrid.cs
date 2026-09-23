using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Initial board, chosen reel stops, and the next unused symbol-instance ID.</summary>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="ReelStops">Zero-based initial stops in reel order.</param>
/// <param name="NextInstanceId">Next unused evaluation-local symbol-instance ID.</param>
public sealed record GeneratedGrid(SymbolBoard Grid, ImmutableArray<int> ReelStops, long NextInstanceId);
