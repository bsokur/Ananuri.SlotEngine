
namespace Ananuri.SlotEngine.Grid;

/// <summary>Evidence that an existing symbol instance moved without changing its identity or symbol.</summary>
public sealed record SymbolMovement(long InstanceId, GridPosition From, GridPosition To);
