# Start here: your first engine run

This guide assumes you have never used this engine. You can run the examples before
learning C#.

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

| Term | Meaning |
| --- | --- |
| Reel / stop | A circular symbol list and the index of its top visible symbol. |
| Board / grid | Visible cells addressed as `[reel, row]`; both indices start at zero. |
| Payline / paytable | One row selected on each reel / awards for matching symbols and lengths. |
| Line stake | Calculation stake divided equally among all configured paylines. |
| Cascade | Remove winning cells, let survivors fall, refill, then evaluate the next grid. |
| Spin | One paid or free spin, including all its cascades. |
| Paid round | A paid spin and every free spin it awards, including retriggers. |
| Raw / payable payout | Amount before / after applying the remaining round cap. Both are gross amounts, without subtracting stake. |
| Continuation / bonus state | Resume the same unfinished spin / start the next free spin. |
| RTP | Gross payouts divided by paid stakes across a distribution or sample. |

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
If the SDK is unavailable, check `dotnet --list-sdks` and install the pinned version.
Open the existing `.sln` file, not an old `.slnx` Recent entry. A missing solution
usually means the terminal or IDE points at another location.

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

The example has three reels, two rows, and one payline on row 1. At stake 100,
the paid board has three scatters above three A symbols. A pays `100 * 5 = 500`;
the scatters request two free spins. A is removed, the scatters fall, and C refills
the top row. That next grid loses, completing the paid spin and activating the bonus.

Each free spin starts with B on the line. B's base award is `100 * 3 = 300`.
The bonus multiplier applies its current value, then increases after a paying grid
and persists into the next free spin. The two spins therefore pay 300 at 1x and 600
at 2x. Each refills to a losing grid. Total gross payout is `500 + 300 + 600 = 1400`;
only the original 100-unit stake was charged. Do not add cumulative round totals
`500 + 800 + 1400` as separate payouts.

To explore the implementation now, follow the [source walkthrough](source-walkthrough.md).
It provides debugger breakpoints and expected state for this exact round.

Then follow this order:

1. [Integrate the engine](integration.md): call it from a separate console application.
2. [Read results and state](results.md): understand what to display, persist, and total.
3. [Configuration reference](configuration-reference.md): change game data and look up JSON settings.
4. [Architecture](architecture.md) and [engine rules](engine-rules.md): explore the implementation and precise semantics.

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
