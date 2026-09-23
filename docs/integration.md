# Call the engine from your own application

The [tutorial command](start-here.md) already runs a complete local host example.
Its full source is [TutorialExample.cs](../samples/TutorialExample.cs), which is compiled
by the simulator and tested by xUnit. This guide shows how to use the same example
from a separate console project, then explains the application boundary.

## A runnable console integration

Run these PowerShell commands from the engine solution folder. Choose a different
output folder if `../SlotEngineTutorialHost` already contains your work.

```powershell
dotnet new console --framework net10.0 -o ../SlotEngineTutorialHost
dotnet add ../SlotEngineTutorialHost/SlotEngineTutorialHost.csproj reference ./src/Ananuri.SlotEngine/Ananuri.SlotEngine.csproj
Copy-Item ./samples/TutorialExample.cs ../SlotEngineTutorialHost/TutorialExample.cs
Copy-Item ./samples/FirstGame.json ../SlotEngineTutorialHost/FirstGame.json
Copy-Item ./global.json ../SlotEngineTutorialHost/global.json
```

Replace that new project's `Program.cs` with this complete program:

```csharp
using Ananuri.SlotEngine.Samples;

if (args.Length != 1)
    throw new ArgumentException("Pass the path to FirstGame.json.");

TutorialExample.Run(args[0]);
```

Run it from the original engine solution folder:

```powershell
dotnet run --project ../SlotEngineTutorialHost -- ../SlotEngineTutorialHost/FirstGame.json
```

You should see the same 500, 300, and 600 spin payouts, 1,400 final total, and successful
18-draw replay shown in [the worked round](first-round.md). The JSON path is relative
to the terminal's current directory, not the project file. An absolute path works too.

The copied sample is teaching code, not part of the library's public NuGet API. The
project reference is convenient for this local exercise. An independently deployed
host should instead consume a pinned released NuGet package and retain its exact artifact.
The verification script also builds and runs both complete programs on this page
against the generated NuGet package, using a temporary host and an isolated cache.
This checks that the packaged API and bundled tutorial work without a project reference.

## How the example is assembled

| Part of `TutorialExample` | What it does |
| --- | --- |
| `Run` | Reads the package file, compiles a reusable definition, and selects console output. |
| `EvaluateRound` | Creates the stateless engine, chooses the smallest allowed stake, and starts the paid spin. |
| `FirstChoiceDrawSource` | Implements every requested choice by returning 0. It is deliberately scripted. |
| `RecordingDrawSource` | Captures draw requests and values in memory and reuses a value for a repeated identical request. |
| `Complete` | Evaluates one grid at a time, serializes/restores checkpoints, resumes, and combines step evidence. |
| `NextBonus` loop | Serializes/restores the returned bonus and passes it to a new free-spin request. |
| `ReplayDrawSource` loop | Recalculates each evaluation from its recorded draws and compares full serialized results. |

`GamePackageLoader.Load` is the JSON-to-definition boundary. Load once and reuse the
definition for many requests. Its components are immutable. The engine itself holds
no player state, so separate calls can share the same engine and definition.

## Paid, paused, free, and finished are different states

```text
SpinRequest with Bonus = null
    -> Evaluate paid spin
    -> if Continuation exists: persist and Resume the SAME spin
    -> when complete, inspect NextBonus
    -> if NextBonus exists: start a NEW SpinRequest with that Bonus
    -> resume that free spin as needed
    -> repeat until a complete result has no NextBonus
```

Use a new evaluation ID for each new paid or free spin. Keep the same ID when retrying
or resuming a spin. Continuations retain the original request and next draw ordinal.
The example's fixed IDs are appropriate for one isolated local run only.

A `WorkBudget` result is unfinished. It does not consume a free spin, start a new
bonus, or ask for another stake. `Steps` contains only the current call's steps,
whereas its spin and round payout totals are cumulative. The example combines steps
but takes totals from the last result. See [results and state](results.md).

## Supplying draws

`IRandomDrawSource.Next(DrawRequest)` must return an integer in
`0 <= value < ExclusiveUpperBound`. A request includes evaluation ID, ordinal,
purpose (such as `initial/reel/0`), and bound. Both initial stops and refills use this
ordered sequence. Invalid values cause an error, not a zero-payout result.
A source may be limited to predetermined requests and reject missing or mismatched
ones. Choose a source that covers the game's required requests; for example,
`FixedReelStops` supplies initial stops but cannot supply cascade refills.

For uniform initial stops and the configured refill probabilities, the host must
supply uniform bounded choices. The engine verifies bounds, not the distribution.
The simulator uses seeded `System.Random` for offline sampling; the tutorial always
uses zero. Neither is the production random-allocation implementation.

In a funded host, durably assign each requested draw before allowing a retry to
request another value. A crash must not change an already selected outcome.
`RecordingDrawSource` and `ReplayDrawSource` are in-memory utilities; they are not
databases or a crash-recovery service.

## Persistence and settlement responsibilities

The host must store the exact game/engine identity, the original request, durable draw
allocations, any continuation, the final result, and next bonus state. Preserve full
state JSON using `SpinStateSerializer`; do not omit zeros or null `Bonus` values.
Version and fingerprint mismatches are errors. State is trusted host data, not a
player-editable token. Protect concurrent access so a bonus spin is consumed once.

The engine returns amounts; it never debits or credits money. For the worked example,
the paid request describes a 100-unit stake, free requests describe zero charged stake,
and the completed round payout is 1,400. Choose a host settlement scheme and apply it
idempotently. Do not repeatedly debit `ChargedStakeUnits` on resumed responses, and
do not settle both individual spin payouts and the cumulative round payout.

Unexpected computation errors must remain errors. Do not catch an exception and
substitute a loss, issue new draws, or discard an active bonus. Recovery must use
the retained request, state, and historical artifacts. The library does not supply
HTTP endpoints, authentication, a wallet, or durable persistence.

## A complete plain-payline example

A one-board game uses the same `SlotEngine`. Omitted components default to ordinary
payline evaluation, no cascades, no features, a constant 1x multiplier, and no payout
cap. The following can replace `Program.cs` in the console project above and needs
no JSON file or custom draw-source class:

```csharp
using Ananuri.SlotEngine;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

var a = new SymbolId(1);
var c = new SymbolId(3);
var game = new GameDefinition(
    "fixed-example", "1", 1, [a, c],
    [new ReelStrip([a, c]), new ReelStrip([a, c]), new ReelStrip([a, c])],
    [new Payline(0, [0, 0, 0])],
    [new PaytableEntry(a, 3, 5)], new BetDefinition([100]));

ISlotEngine engine = new SlotEngine();
var request = new SpinRequest("plain-example/0", 100);
var result = engine.Evaluate(game, request, new FixedReelStops(game, [0, 0, 0]));
Console.WriteLine($"Payout: {result.TotalPayoutUnits}");
```

Expected output is `Payout: 500`. `result.Steps[0].Grid` is the board, and
`result.Steps[0].Awards` contains its ordinary award evidence. The result is complete
after one grid, with no continuation or next bonus.

`FixedReelStops` validates one in-range stop for each reel. It implements initial
stop requests only and rejects refill requests. When providing stops for a free
spin, construct it with `SpinMode.Free` so it validates against the free-spin strips.
The supplied stops are known teaching inputs; a host must select stops according
to its intended random model. Use a general `IRandomDrawSource` for cascades.
Read [plain-payline rules](plain-payline-rules.md) before adapting this example.

## Configuring the one engine

`GameDefinition` lives in `Ananuri.SlotEngine.Definitions`; the engine lives in
`Ananuri.SlotEngine`. Requests and results use `Ananuri.SlotEngine.Spins`, while
draw sources use `Ananuri.SlotEngine.Randomness`.
Its required constructor arguments are game ID, math version, visible rows, symbols,
reels, paylines, paytable, and bet definition. It directly accepts these optional
components through named arguments:

| Argument | Default / purpose |
| --- | --- |
| `cascades` | `NoCascades`; select `FallingSymbolsCascade` to remove and refill wins. |
| `winLimit` | `NoWinLimit`; select `TotalStakeWinLimit` to cap the paid round. |
| `wins` | Ordinary payline evaluator; configurable substitution and award selection. |
| `gridGenerator` | Circular reel-strip initial boards. |
| `scatters` | No scatter awards. |
| `features` | No free-spin feature. |
| `paidMultiplier`, `bonusMultiplier` | Constant 1x. |
| `freeSpinReels` | Reuse paid reels. |
| `multiplyScatterAwards` | `false`; whether the multiplier also affects scatter money. |
| `allowScatterRefills` | `false`; explicit permission for scatters in replacement tables. |

Composition is fixed when the definition is constructed. Adding a mechanic changes
configuration, not the engine class, request type, or result type. For a complete
configuration assembled from JSON, use the tutorial and [field reference](configuration-reference.md).
For custom C# evaluators, paylines and the paytable may be empty when unused.
Custom policies must declare their symbol roles, capabilities, and payout bounds;
see [the developer guide](implementation-guide.md) before implementing one.
