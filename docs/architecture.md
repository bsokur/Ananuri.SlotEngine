# Solution architecture

This is the developer's code map. New readers should first follow [Start here](start-here.md).
To follow the teaching round in a debugger,
use the [source walkthrough](source-walkthrough.md). The engine, simulator, and tests have one-way dependencies:

```text
Simulator ──► SlotEngine
Tests ──► SlotEngine, Simulator
```

The engine calculates outcomes, and the simulator exercises them offline. Tests verify
the library and simulator. The simulator's sample game definitions live in JSON. It loads
copies from its output folder; tests embed
the same JSON files as resources. These samples are not engine dependencies.
The [custom-policy example](../samples/CustomMultiplierExample.cs) separately composes a teaching game in C#;
its source is compiled into tests and shipped with the package.

## Solution Explorer

| Solution root item | Contents |
| --- | --- |
| Ananuri.SlotEngine | The reusable engine library project |
| Ananuri.SlotEngine.Tests | Tests grouped by mechanic, with shared fixtures |
| Ananuri.SlotEngine.Simulator | The offline simulator project |
| Samples | JSON game definitions and the executable tutorial host example |
| Documentation | Setup, architecture, rules, configuration, integration, results, and game math |

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
3. Delegate raw awards, the remaining round limit, and cumulative totals to `GridPayoutCalculator`.
4. Select removals. If the spin continues, apply gravity and refill through the draw sequence.
5. Return the recorded step and updated continuation state.

With `NoCascades`, the evaluated board is the final grid even when it pays. With a
cascade policy, the next board returns through the same evaluator. There is one
payline implementation for both configurations.

The coordinator completes the free-spin feature after the final grid, or returns a checkpoint
when the work budget is exhausted. Execution responsibilities have separate owners:

| Component | Responsibility |
| --- | --- |
| `SlotEngine` | Spin lifecycle, work budget, and feature completion |
| `GridStepEvaluator` | Ordered policy calls, cascade execution, and next-grid state |
| [GridPayoutCalculator](../src/Ananuri.SlotEngine/Execution/GridPayoutCalculator.cs) | Raw award evidence, payable amount after the round cap, and cumulative spin/round totals |
| [SpinRequestValidator](../src/Ananuri.SlotEngine/Execution/SpinRequestValidator.cs) | Request identity, supported stake, and bonus compatibility |
| [SpinContinuationValidator](../src/Ananuri.SlotEngine/Execution/SpinContinuationValidator.cs) | Checkpoint identity, counters, totals, reel stops, and tracked instances |
| [SymbolBoardValidator](../src/Ananuri.SlotEngine/Execution/SymbolBoardValidator.cs) | Board dimensions, declared symbols, and instance sequence, shared by checkpoints and cascades |
| [WinAwardValidator](../src/Ananuri.SlotEngine/Execution/WinAwardValidator.cs) | Ordinary award positions, substitutions, and declared payout bounds |
| [ScatterResultValidator](../src/Ananuri.SlotEngine/Execution/ScatterResultValidator.cs) | Scatter evidence, counted instances, payout bounds, and feature capability |
| `MultiplierResultValidator` | Multiplier bounds and collection evidence before updating the collected-instance set |
| `CascadeTransitionValidator` | Removals, survivors, fresh IDs, movements, and arrivals against both boards |
| `FeatureTransitionValidator` | Outgoing grants and bonus state at spin completion |

These helpers are internal; the public policy interfaces remain the extension points.
The payout calculator does not advance multiplier/bonus state or consume draws. Coordinators
retain ordering and lifecycle decisions instead of distributing the workflow among validators.
`SpinStateSerializer` provides strict JSON loading for persisted continuations and bonus state.
The separation preserves evaluation order, random draw order, multiplier timing, cap handling,
and fingerprint calculation for valid inputs.

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
For plain-payline runs, `FixedPaylineSimulation` coordinates execution, `PaylineStatistics`
accumulates outcomes, and `PaylineReport` formats the demo and summary output.
`game-demo` and `game-simulate` accept packages with or without cascades. Scripted replay runs
after the processing timer stops. A finite grid and spin budget covers the paid spin
and its entire bonus. Exceeding either budget or cancelling stops the command with
an error; an incomplete round is never included in a successful simulation report.

The `tutorial` command calls the linked `samples/TutorialExample.cs` host example with
the bundled `FirstGame.json`. It intentionally uses scripted choices and a one-grid work
budget to demonstrate JSON checkpoints, free-spin persistence, and complete replay.
The verification script runs this command and also compiles the shipped example against
the generated NuGet package to check its documented results.

## Tests and reading order

Tests are grouped into `FixedPaylines`, `Cascading`, `Grid`, `Wilds`, `Scatters`, `FreeSpins`,
`Multipliers`, `Limits`, `Randomness`, `Configuration`, and `Simulator`. `Fixtures/EngineFixtures.cs`
contains shared definitions and deterministic draw sources.

The [guided debugger path](source-walkthrough.md) gives breakpoints and expected values
for the paid win, checkpoint, and free-spin completion. For a broader code tour:

1. Open `samples/FirstGame.json` and `Definitions/GameDefinition.cs`.
2. Read `Spins/SpinRequest.cs`, `Spins/SpinEvaluation.cs`, and `FreeSpins/BonusState.cs`.
3. Follow `SlotEngine.cs` into `Execution/GridStepEvaluator.cs`.
4. Read the interface and implementation for the mechanic you want to change, then its tests.
5. Follow the simulator's `Commands/GameSimulation.cs` to see complete-round execution.

For exact behavior and integration examples, continue with [engine rules](engine-rules.md)
and [integration](integration.md).

## Extend a policy

Prefer changing [game configuration](configuration-reference.md) for data-only changes.
For a new mechanic, specify timing, symbol roles, payout bounds, state, and draw consumption
before implementing the corresponding interface. Keep policies immutable and deterministic;
include all behavior settings and an implementation version in `ConfigurationKey`.
Do not use hidden mutable state, clocks, or unrecorded randomness.

| Interface | Declarations checked during composition |
| --- | --- |
| `IWinEvaluator` | Paying symbols, substitution symbols, and `MaximumBasePayout(game, calculationStake)` |
| `ISymbolSubstitutionPolicy` | Every symbol involved in substitution |
| `IScatterEvaluator` | Scatter symbols, free-spin capability, and maximum award multiplier |
| `IMultiplierStrategy` | Collection symbols, multiplier bounds, and persistence |
| `ICascadePolicy` / `IRefillPolicy` | Every possible replacement symbol in both modes |
| `IFeaturePolicy` | Whether free spins are supported |

Return immutable sets, including empty sets for unused roles. An evaluator's `BigInteger`
bound covers the sum of ordinary base awards for one grid at each allowed stake, before
feature multipliers. It must be conservative even when a round cap is configured.
Execution also validates policy output; invalid output raises an error, not a losing result.

[CustomMultiplierExample.cs](../samples/CustomMultiplierExample.cs) is a complete example:
`FirstGridMultiplier` applies a configured factor on grid 0 and 1x on subsequent grids.
It uses the persisted grid index rather than mutable flags. `AppliedMultiplier` controls
the current award; `NextState` is retained separately. Both must respect the declared bounds.
Include the sample source in a console project referencing the library, then run:

```csharp
using Ananuri.SlotEngine.Samples;

var result = CustomMultiplierExample.Run();
Console.WriteLine($"Payout: {result.TotalPayoutUnits}");
```

The fixed example pays `200 * 3 + 100 * 1 = 700` units across three grids and nine draws.
The final grid loses. These scripted choices demonstrate the policy, not RTP.
The sample's tests compare full evaluation with serialized resumptions using fresh policies.

Direct C# composition accepts custom policies. JSON intentionally selects reviewed built-ins;
adding a name alone does not load a class. JSON support requires explicit DTO/compiler,
validation, and reference updates. Not every future mechanic fits the current board and
free-spin lifecycle; define new contracts when needed rather than duplicating the engine.

## Verify a change

Use independently calculated expected results and test observable contracts, especially
award arithmetic, caps, feature timing, rejected state, and full versus resumed replay.
Run `./scripts/verify.ps1` for Release build, formatting, tests, deterministic simulator
scenarios, documentation links, package contents, and runnable packaged examples.
Simulation regressions are not proof of exact game mathematics or production readiness.

Changing award selection, draw ordering, timing, or state can break historical replay.
Version changed behavior and retain exact prior packages and engine artifacts. Never bypass
compatibility checks by replacing fingerprints on an active bonus or checkpoint.
Update the appropriate rule, configuration, or result reference alongside contract changes.
The host remains responsible for durable random allocation, concurrency, recovery, and settlement.
