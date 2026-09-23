# Start here: your first engine run

This guide assumes you have never used this engine. You can run the examples before
learning C#. The [glossary](glossary.md) explains the terms used throughout the guides.

## What you have

Ananuri.SlotEngine is a calculator for slot-game outcomes. Give it a game definition,
a stake, and externally selected numbers, and it returns the board, awards, payout,
and any next free-spin state. It has no screen, spinning animation, player account,
wallet, database, or web server.

The **library** contains that calculator. The **simulator** calls it from a console
and prints results. The **tests** check results against expected values.
`Ananuri.SlotEngine.sln` groups the projects for development.

There is one entry point: `SlotEngine`, implementing `ISlotEngine`. Every game uses a
`GameDefinition`, a `SpinRequest`, and a draw source. The returned `SpinEvaluation`
describes the outcome. Paylines decide which symbols win; cascades decide whether a
winning board is replaced and evaluated again. They are independent settings.

| Game behavior | Configuration |
| --- | --- |
| Paylines on one board | `NoCascades`, the default for a directly constructed definition |
| Remove winning symbols, refill, and evaluate again | `FallingSymbolsCascade` |
| Gross payout without a game cap | `NoWinLimit`, the default |
| Gross payout capped across a paid round | `TotalStakeWinLimit` |

Wilds, scatters, free spins, and multipliers are optional components of the same
definition. This learning path demonstrates several together. For a smaller first
integration, see the [plain-payline example](integration.md#a-complete-plain-payline-example).
Library calls are synchronous calculations. The console examples need no server.

## 1. Prepare your tools

Use a Windows terminal running PowerShell, or PowerShell on another supported .NET
platform. Install **.NET SDK 10.0.400**, the exact version selected by the repository's
`global.json`. The SDK includes build tools; installing only the runtime is insufficient.
A compatible IDE is optional for the command-line steps.

Open the folder containing `Ananuri.SlotEngine.sln` in your terminal. All commands in
this learning path start there unless stated otherwise. Run:

```powershell
dotnet --version
Get-Location
Test-Path ./Ananuri.SlotEngine.sln
```

The first result should be `10.0.400`; the final result should be `True`.
If not, use [troubleshooting](troubleshooting.md) before continuing.

## 2. Build and test

Run these commands one at a time and stop if a command fails:

```powershell
dotnet restore Ananuri.SlotEngine.sln
dotnet build Ananuri.SlotEngine.sln -c Release --no-restore
dotnet test Ananuri.SlotEngine.sln -c Release --no-build --no-restore
```

Restore obtains the test packages from NuGet. Build compiles source into executable
code. Test runs xUnit's checks. A successful build reports zero errors; a successful
test run reports no failed tests. Internet access can be needed for restore.

To use Visual Studio instead, open the solution, build it, and use **Test Explorer**
to run the tests. You do not need to recreate or rearrange the existing projects.

## 3. Run the teaching example

```powershell
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- tutorial
```

`--project` selects the console application. Everything after `--` is an argument to
that application. This command loads [FirstGame.json](../samples/FirstGame.json) and
runs [TutorialExample.cs](../samples/TutorialExample.cs). Expected output:

```text
tutorial/0: mode=Paid; charged=100; spin payout=500; round payout=500; remaining free spins=2
tutorial/1: mode=Free; charged=0; spin payout=300; round payout=800; remaining free spins=1
tutorial/2: mode=Free; charged=0; spin payout=600; round payout=1400; remaining free spins=0
Replay verified: 3 evaluations; 18 draws.
```

Only the paid spin has a charged stake. Each free spin has its own payout; the round
total includes all three spins. These are scripted teaching results. The example
always selects the first available choice, so this run does not estimate RTP.

## 4. Understand what happened

Read [one complete round](first-round.md). It draws the boards, shows each calculation,
and explains why the second free spin has a larger payout than the first.

Then follow this order:

1. [Create your first game](create-a-game.md): change one rule and predict the output.
2. [Integrate the engine](integration.md): call it from a separate console application.
3. [Read results and state](results.md): understand what to display, persist, and total.
4. [Configuration reference](configuration-reference.md): look up every JSON setting.
5. [Architecture](architecture.md) and [cascading rules](engine-rules.md): explore the implementation and precise semantics.

## Other commands you will encounter

| Command after `--` | Purpose |
| --- | --- |
| `demo` | One plain-payline example: 100-unit stake, 500-unit payout |
| `enumerate [package.json] [--max-combinations N]` | All initial stop combinations of a one-board game; defaults to the tiny fixture's 64 combinations and exact 87.5% RTP |
| `simulate 10000 12345` | Sample 10,000 tiny-game spins using seed 12345 |
| `game-demo [package.json]` | Script the first paid board, complete any bonus, verify replay |
| `game-simulate 10000 12345 [package.json]` | Sample 10,000 complete rounds for any supported package |
| `tutorial [package.json]` | Always choose zero; explainable local round with checkpoint and replay |

Square brackets here mean an optional argument; do not type the brackets. Defaults
for game demo/simulation use `CascadingGame.json`; tutorial uses `FirstGame.json`.
`demo` uses `FiveReelGame.json`; `enumerate` and `simulate` use `PaylineGame.json`.
The build copies these files into the simulator's `Samples` output folder, where
default commands load them. Explicit file arguments are relative to your terminal's current folder.

Exact enumeration uses the package's actual reel lengths and smallest allowed stake.
It rejects cascading/free-spin games and more than 1,000,000 combinations by default;
`--max-combinations N` changes that positive limit. Changing a reel changes the
enumerated combinations automatically.

`game-demo` and `game-simulate` allow at most 10,000 grids and 1,000 spins per paid
round, including every free spin. Set positive limits with `--max-grids-per-round N`
and `--max-spins-per-round N` when deliberately studying a larger round. Reaching
either limit fails the command with the round unfinished; truncated payouts are
never reported as a complete simulation. Ctrl+C cancels execution.

To see the same engine run a plain game, pass `samples/PaylineGame.json` to
`game-demo` or `game-simulate`. This is the tiny fixture used by `enumerate`, with
one board per spin, no free spins, and no payout cap.

For all verification steps plus local package creation, run `./scripts/verify.ps1`
from PowerShell. It can also be invoked by absolute path from another folder. It
stops at the first failed command. PowerShell and .NET are the only required runtimes.

CascadingGame is experimentally calibrated; the other samples are untuned teaching
fixtures. None is independently certified. See [simulation and game math](simulation-and-math.md)
for larger runs, payout targets, and measured volatility.
Building a commercial game includes separate mathematical design and host integration;
running these examples does not provide either.
