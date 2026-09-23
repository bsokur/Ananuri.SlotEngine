using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Evidence that a particular symbol instance contributed a configured multiplier increment.</summary>
/// <param name="InstanceId">Evaluation-local symbol-instance identity.</param>
/// <param name="Symbol">Game-scoped symbol identifier.</param>
/// <param name="Position">Zero-based grid position.</param>
/// <param name="Value">Configured positive multiplier increment from this symbol; the actual state increase may be smaller at the cap.</param>
public sealed record SymbolCollection(long InstanceId, SymbolId Symbol, GridPosition Position, long Value);
