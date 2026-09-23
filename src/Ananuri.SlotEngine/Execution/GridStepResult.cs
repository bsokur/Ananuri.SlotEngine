using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Execution;

internal sealed record GridStepResult(SpinContinuation State, CascadeStep Step, bool IsComplete, bool WinLimitReached);
