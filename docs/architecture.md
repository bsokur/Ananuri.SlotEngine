# Solution architecture

This is the developer's code map. New readers should first follow [Start here](start-here.md)
and [one complete round](first-round.md). The engine, simulator, and tests have one-way dependencies:

```text
Simulator ──► SlotEngine
Tests ──► SlotEngine, Simulator
```

The engine calculates outcomes, and the simulator exercises them offline. Tests verify
the library and simulator. Sample game definitions live in JSON. The simulator loads
copies from its output folder; tests embed
the same JSON files as resources. These samples are not engine dependencies.

## Solution Explorer

| Solution root item | Contents |
| --- | --- |
| Ananuri.SlotEngine | The reusable engine library project |
| Ananuri.SlotEngine.Tests | Tests grouped by mechanic, with shared fixtures |
| Ananuri.SlotEngine.Simulator | The offline simulator project |
| Samples | JSON game definitions and the executable tutorial host example |
| Documentation | This guide, rule specifications, integration guidance and verification results |

The projects appear directly under the solution. Samples and Documentation are
solution navigation folders. The physical project paths remain `src/`, `tests/`, and `tools/`.

## Engine organization

One `ISlotEngine` / `SlotEngine` coordinates every game. `Definitions/GameDefinition.cs`
captures both the base mathematics and selected components; there is no inner base-game
wrapper. Plain games and games with cascades use the same `SpinRequest`, `SymbolBoard`,
`SpinEvaluation`, and award records. `NoCascades` and `NoWinLimit` are the defaults.

Feature folders sit directly under the library root. Each public type has its own named file.
An interface sits alongside the implementations and data types for its feature:

| Folder | Responsibility |
| --- | --- |
| Definitions | Immutable game data, primitives, and composed `GameDefinition` |
| Definitions/Validation | Base game-data invariants such as reel, payline, and paytable consistency |
| Configuration | Compose a reusable game definition, parse JSON, compile packages and calculate identity |
| Configuration/Packages | JSON transport types, copied into validated domain objects |
| Configuration/Validation | Check component compatibility and arithmetic bounds at construction |
| Execution | Internal single-grid calculations and request/continuation/result validation |
| Grid | Grid positions and boards; generate boards, select removals, apply gravity and refill symbols |
| Wins | Ordinary-award contract, evaluator payout bounds, and built-in payline evaluation |
| Wilds | Decide which ordinary symbols a wild may substitute for |
| Scatters | Count eligible scatter instances and calculate threshold awards |
| FreeSpins | Validate bonus state, grant/retrigger spins and complete bonus transitions |
| Multipliers | Apply constant, paying-cascade or collected-symbol strategies |
| Limits | Calculate the maximum gross payout for a complete paid round |
| Randomness | Request, record and replay bounded random draws |
| Spins | Requests, grid-step results, evaluations and resumable checkpoints |

## Public namespaces

Public namespaces follow the feature folders within the same library:

| Namespace | Examples |
| --- | --- |
| `Ananuri.SlotEngine` | `ISlotEngine`, `SlotEngine` |
| `Ananuri.SlotEngine.Definitions` | `GameDefinition`, `SymbolId` |
| `Ananuri.SlotEngine.Configuration` | `GamePackageLoader`, `IGameComponent` |
| `Ananuri.SlotEngine.Configuration.Packages` | JSON package DTOs |
| `Ananuri.SlotEngine.Spins` | `SpinRequest`, `SpinEvaluation`, `SpinStateSerializer` |
| `Ananuri.SlotEngine.Randomness` | `IRandomDrawSource`, `FixedReelStops`, `ReplayDrawSource` |
| `Ananuri.SlotEngine.Grid` | `GridPosition`, `SymbolBoard` |
| `Ananuri.SlotEngine.Wins`, `.Wilds`, `.Scatters` | Win, substitution, and scatter policies |
| `Ananuri.SlotEngine.FreeSpins`, `.Multipliers`, `.Limits` | Bonus state, multiplier policies, and payout limits |

## Configuration flow

```text
JSON package
  → GamePackageLoader: strict parsing and duplicate-property rejection
  → GamePackageCompiler: construct the configured built-in policies
  → GameDefinition: capture game data and component composition
      → definition validators: validate data, compatibility, and bounds
      → GameDefinitionFingerprint: calculate canonical content identity
  → reusable immutable definition
```

`GamePackageLoader.Compile` also accepts transport objects directly. Both public entry points
remain on the loader. The compiler is internal. Hosts may compose custom policies directly
through `GameDefinition`; the JSON compiler intentionally supports the built-in profile.
`IGameComponent.ConfigurationKey` identifies a component's rules and configuration for hashing.
Policy interfaces declare paying, substitution, scatter, collection, and refill symbols,
plus feature capabilities. Compatibility checks use those declarations rather than
concrete implementation types. Each evaluator supplies its own ordinary payout bound;
validation combines it with multiplier and scatter bounds for every allowed stake.
The built-in payline evaluator owns payline-specific requirements.

## Spin execution flow

`SlotEngine` coordinates the lifecycle and work budget. It validates the request,
generates the initial board, chooses the paid/free-spin multiplier strategy and enters `Resume`.
`Resume` validates the checkpoint and delegates each grid to `GridStepEvaluator`:

1. Evaluate scatters, then ordinary wins with configured wild substitution and award selection.
2. Apply the multiplier strategy and track collected symbol instances.
3. Calculate raw awards and, when a cap is configured, apply the remaining round limit.
4. Select removals. If the spin continues, apply gravity and refill through the draw sequence.
5. Return the recorded step and updated continuation state.

With `NoCascades`, the evaluated board is the final grid even when it pays. With a
cascade policy, the next board returns through the same evaluator. There is one
payline implementation for both configurations.

The coordinator completes the free-spin feature after the final grid, or returns a checkpoint
when the work budget is exhausted. `SpinExecutionValidator` checks state and policy output
invariants. `CascadeTransitionValidator` reconciles removals, survivors, fresh IDs, movements,
and arrivals against both boards. `SpinStateSerializer` provides strict JSON loading for
persisted continuations and bonus state. These helpers preserve evaluation order, random draw
order, multiplier timing, cap handling and fingerprint calculation for valid inputs.

The host owns random allocation, persistence and settlement. A `BonusState` carries free-spin
counters and, when configured, the multiplier across spins. A `SpinContinuation` resumes the
same spin between grids. Definitions and the stateless coordinator can be reused; spin state
is supplied explicitly for each evaluation.

## Simulator organization

`Program.cs` sets invariant formatting and dispatches commands.

| Folder | Responsibility |
| --- | --- |
| Commands | Parse game options and run plain-payline or featured scenarios |
| Execution | Complete evaluations within a shared round budget, with cancellation and checkpoint evidence |
| Statistics | Accumulate payouts, frequencies, distributions and round variance |
| Reporting | Format grid-step traces and summary statistics |
| Replay | Verify complete scripted simulator results against recorded draws |
| Randomness | Supply the simulator's seeded offline random draws |

`GameSimulation` drives a paid spin and all its associated free spins before recording a
complete round. `GameStatistics` owns accumulation; `GameReport` owns presentation.
`game-demo` and `game-simulate` accept packages with or without cascades. Scripted replay runs
after the processing timer stops. A finite grid and spin budget covers the paid spin
and its entire bonus. Exceeding either budget or cancelling stops the command with
an error; an incomplete round is never included in a successful simulation report.

The `tutorial` command calls the linked `samples/TutorialExample.cs` host example with
the bundled `FirstGame.json`. It intentionally uses scripted choices and a one-grid work
budget to demonstrate JSON checkpoints, free-spin persistence, and complete replay.
Tests reference the simulator assembly to exercise this same compiled example and check
the documented expectations without compiling a second copy.

## Tests and reading order

Tests are grouped into `FixedPaylines`, `Cascading`, `Grid`, `Wilds`, `Scatters`, `FreeSpins`,
`Multipliers`, `Limits`, `Randomness`, `Configuration`, and `Simulator`. `Fixtures/EngineFixtures.cs`
contains shared definitions and deterministic draw sources.

For a first walkthrough:

1. Open `samples/FirstGame.json` and `Definitions/GameDefinition.cs`.
2. Read `Spins/SpinRequest.cs`, `Spins/SpinEvaluation.cs`, and `FreeSpins/BonusState.cs`.
3. Follow `SlotEngine.cs` into `Execution/GridStepEvaluator.cs`.
4. Read the interface and implementation for the mechanic you want to change, then its tests.
5. Follow the simulator's `Commands/GameSimulation.cs` to see complete-round execution.

For exact behavior and integration examples, continue with [engine rules](engine-rules.md).
The plain-payline configuration contract is [plain-payline-rules.md](plain-payline-rules.md).
Executed checks are recorded in [verification.md](verification.md).
