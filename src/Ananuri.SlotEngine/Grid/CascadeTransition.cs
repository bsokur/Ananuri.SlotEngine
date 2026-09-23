using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Grid;

/// <summary>The resulting board and complete removal, movement, and arrival evidence for a cascade.</summary>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="Removed">Cells removed from the source board.</param>
/// <param name="Movements">All surviving instances whose grid position changed.</param>
/// <param name="Arrivals">All freshly allocated instances and their arrival positions.</param>
/// <param name="NextInstanceId">Next unused evaluation-local symbol-instance ID.</param>
public sealed record CascadeTransition(SymbolBoard Grid, ImmutableArray<GridPosition> Removed,
    ImmutableArray<SymbolMovement> Movements, ImmutableArray<SymbolArrival> Arrivals, long NextInstanceId);
