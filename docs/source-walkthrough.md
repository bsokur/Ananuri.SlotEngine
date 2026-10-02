# Debug one round through the source

Use this guide after [Start here](start-here.md).
It connects those boards and payouts to the methods and state you see in a debugger.
For a first integration without cascades or free spins, start with the
[plain-payline program](integration.md#a-complete-plain-payline-example).

Three scopes matter while stepping through the code:

| Scope | Meaning in this tutorial |
| --- | --- |
| Round | One paid spin plus its two awarded free spins; the final gross payout is 1,400. |
| Spin | One request and evaluation ID, including every cascade it produces. |
| Grid | One board evaluated within a spin. Each tutorial spin evaluates grids 0 and 1. |

A call can stop between grids. `Evaluate` starts a spin; `Resume` continues that same
spin. Neither call necessarily completes a spin or a round.

## Set up the debugger

Open `Ananuri.SlotEngine.sln`, select `Ananuri.SlotEngine.Simulator` as the startup
project, and set its command-line argument to `tutorial`. Debug with breakpoints at
the locations below. The command loads the copied [FirstGame.json](../samples/FirstGame.json)
and calls [TutorialExample.Run](../samples/TutorialExample.cs).
The verification script also runs this example and checks its payouts and replay.

The example's `FirstChoiceDrawSource.Next` always returns zero. That makes every
board predictable. The replay loop executes the round again after the first three
spins; stop debugging before that loop unless you want to inspect replay too.

## 1. Start the paid spin

In `TutorialExample.EvaluateRound`, break on the `Complete` call. The request has
`EvaluationId = "tutorial/0"`, `CalculationStakeUnits = 100`, and `Bonus = null`.
Its mode is `Paid`. `Complete` calls the engine with `maxGridEvaluations: 1`, deliberately
limiting each call to one grid.

Step into [`SlotEngine.Evaluate`](../src/Ananuri.SlotEngine/SlotEngine.cs). After
`game.GridGenerator.Generate`, the board has scatters (symbol 8) in row 0 and A
(symbol 1) in row 1. `SelectMultiplierStrategy` selects `game.PaidMultiplier`, whose
value is 1. The initial `SpinContinuation` passed into `Resume` contains:

| Field | Value |
| --- | --- |
| `InitialStops` | `[0, 0, 0]` |
| `NextGridIndex` | `0` |
| `NextInstanceId` | `6`; the six initial cells have IDs 0 through 5 |
| `NextDrawOrdinal` | `3`; the initial board consumed one stop per reel |
| `Multiplier` | `1` |
| `SpinPayoutUnits`, `RoundPayoutUnits`, `PendingFreeSpins` | All `0` |

This initial state is an internal starting point. No checkpoint has been returned to
the caller yet.

## 2. Evaluate the 500-unit win

Break inside [`GridStepEvaluator.Evaluate`](../src/Ananuri.SlotEngine/Execution/GridStepEvaluator.cs)
and step past each calculation. `state.NextGridIndex` is 0:

| Completed operation | What to inspect |
| --- | --- |
| `game.Scatters.Evaluate` | Three counted scatter instances, zero money, `RequestedFreeSpins = 2`. |
| `game.Wins.Evaluate` | One ordinary award for A: `BasePayoutUnits = 500`, from `100 * 5`. |
| `multiplierStrategy.Apply` | `AppliedMultiplier = 1`, `NextState.Value = 1`. |
| `GridPayoutCalculator.Calculate` | `payout.RawPayoutUnits = 500`, `payout.PayablePayoutUnits = 500`, `payout.WinLimitReached = false`. Step into the calculator to inspect `remainingRoundAllowance = 5000`. |
| `ApplyCascadeIfNeeded` | A instances 1, 3, 5 are removed; scatter instances 0, 2, 4 fall; new C instances 6, 7, 8 enter row 0. |

[`GridPayoutCalculator`](../src/Ananuri.SlotEngine/Execution/GridPayoutCalculator.cs)
owns the award arithmetic and round-cap calculation; `GridStepEvaluator` uses its result
to decide whether to cascade and which totals to retain.

The two requested free spins are still pending. The feature grants them only after
the paid spin finishes. The returned step records the board that just paid and its
transition; the updated state holds the replacement board to evaluate next.
Inspect `step.Grid` and `nextState.Grid` side by side to see that boundary.

## 3. Inspect and resume the checkpoint

Back in `TutorialExample.Complete`, break immediately after `engine.Evaluate`.
The call used its one-grid budget. `result.CompletionReason` is `WorkBudget`,
`result.IsComplete` is false, and `result.NextBonus` is null. Inspect `result.Continuation`:

| Field | Value |
| --- | --- |
| `Request.EvaluationId` | `"tutorial/0"`, unchanged |
| `Grid` | C (3) in row 0, scatter (8) in row 1 |
| `NextGridIndex` | `1` |
| `NextInstanceId` | `9` |
| `NextDrawOrdinal` | `6`; three stops plus three refill draws |
| `Multiplier` | `1` |
| `CountedScatterInstanceIds` | `{0, 2, 4}` |
| `SpinPayoutUnits`, `RoundPayoutUnits` | Both `500` |
| `PendingFreeSpins` | `2` |

This **continuation** resumes the current spin, with its existing request, board, and
draw position. `Complete` serializes and restores it using `SpinStateSerializer`, then
calls `SlotEngine.Resume`. Grid 1 has no ordinary award; the initial-grid-only scatter
rule does not trigger again. No removals remain, so the spin finishes without more draws.

In `SlotEngine.Resume`, step into `game.Features.Complete`, implemented by
[`FreeSpinsFeature.Complete`](../src/Ananuri.SlotEngine/FreeSpins/FreeSpinsFeature.cs).
It converts the two pending spins into a `BonusState`. `CreateSpinEvaluation` returns
`CompletionReason.NoMoreWins`, no continuation, and the new `NextBonus`:

`OriginatingRoundId = "tutorial/0"`, `RemainingSpins = 2`, `TotalSpinsAwarded = 2`,
`CompletedSpins = 0`, `Multiplier = 1`, and `RoundPayoutUnits = 500`.

This **bonus state** starts the next spin, with a new request and evaluation ID. It
does not resume the paid spin. `Complete` combines each call's `Steps`, but takes
cumulative payout totals from the last result. Adding the two returned 500-unit
totals would count the same win twice.

## 4. Finish both free spins

Return to `TutorialExample.EvaluateRound`. It serializes/restores `NextBonus`, then
creates request `"tutorial/1"`. Because the request carries a bonus, its mode is `Free`
and `ChargedStakeUnits` is 0. `CalculationStakeUnits` stays 100.

In `SlotEngine.Evaluate`, `SelectMultiplierStrategy` now selects `game.BonusMultiplier`.
In [`PayingCascadeMultiplier.Apply`](../src/Ananuri.SlotEngine/Multipliers/PayingCascadeMultiplier.cs),
inspect both the applied value and the next state: a paying grid uses the current
factor, then advances the factor for the next grid. A losing grid does not advance it.

| Request | Paying grid calculation | Completed spin payout | Cumulative round payout | `NextBonus` |
| --- | --- | ---: | ---: | --- |
| `tutorial/1` | B: `100 * 3 * 1 = 300`; factor becomes 2 | 300 | 800 | 1 remaining, 1 completed, factor 2 |
| `tutorial/2` | B: `100 * 3 * 2 = 600`; factor becomes 3 | 600 | 1400 | `null` |

Each spin again stops after grid 0, resumes onto a losing all-C board, and completes
in `FreeSpinsFeature.Complete`. The completed final result has neither a continuation
nor a next bonus, so the round loop ends. Replay then repeats all three evaluations
using the 18 recorded draws and compares the full results.

## Tests to read next

The verification script checks the tutorial's spin payouts, bonus counters, and
18-draw replay. For focused contracts, read:

- [CheckpointBoundaryTests](../tests/Ananuri.SlotEngine.Tests/Cascading/CheckpointBoundaryTests.cs): checkpoints retain pending feature awards until the spin completes.
- [`CascadingTests.WorkBudget_ShouldResumeWithIdenticalResultAndDrawOrder_AfterJsonRoundTrip`](../tests/Ananuri.SlotEngine.Tests/Cascading/CascadingTests.cs): full execution versus resumed execution with several budgets.
- [`FreeSpinsTests.PaidSpin_ShouldFinishCascadesBeforeStartingBonus_AndRemoveSharedPositionsOnce`](../tests/Ananuri.SlotEngine.Tests/FreeSpins/FreeSpinsTests.cs): pending grants become a bonus only at spin completion.
- [`FreeSpinsTests.Bonus_ShouldPayOldMultiplierThenIncreaseOncePerGrid_AndCarryAcrossFreeSpins`](../tests/Ananuri.SlotEngine.Tests/FreeSpins/FreeSpinsTests.cs): multiplier timing and persistence with different scripted boards.

Use [architecture](architecture.md) to find another component, [results and state](results.md)
for the full field reference, and [extension guidance](architecture.md#extend-a-policy)
before changing a rule.
