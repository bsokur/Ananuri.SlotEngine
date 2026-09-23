# Ananuri.SlotEngine

A .NET library that calculates slot-game boards, awards, payouts, cascades, and
free-spin state from a game definition and externally supplied choices. This repository
also contains a console simulator, executable examples, and xUnit tests.

Initial release: **0.1.0**, with JSON package schema **1** and rule profile `slot-engine-v1`.

**New to the engine? Begin with [Start here](docs/start-here.md).** It explains the
tools, runs a predictable example, and takes you through one complete paid round.
No knowledge of the source code is needed to start.

## Learn in this order

| Guide | What you learn |
| --- | --- |
| [Start here](docs/start-here.md) | What the library does, setup, build, test, and your first run |
| [Simulation and game math](docs/simulation-and-math.md) | Run simulations, interpret RTP and volatility, and review current game settings |
| [Terminology](docs/glossary.md) | Reels, stops, paylines, stakes, cascades, RTP, and state |
| [One complete round](docs/first-round.md) | Actual boards and calculations: a paid spin plus two free spins |
| [Create your first game](docs/create-a-game.md) | Edit a package, predict the change, and verify it |
| [Integrate the engine](docs/integration.md) | A runnable separate console application and host responsibilities |
| [Results and state](docs/results.md) | Every result/state field and how to interpret totals |
| [Configuration reference](docs/configuration-reference.md) | Every JSON field, default, restriction, and strategy |
| [Troubleshooting](docs/troubleshooting.md) | Setup, package validation, replay, and persistence errors |

## Run it

These development commands apply to the source checkout, not an installed NuGet
package. The package includes the guides and sample data/source; code-map links into
the library source also require the checkout.

From the folder containing this README, using the .NET SDK 10.0.400 pinned in
[global.json](global.json), run these commands one at a time:

```powershell
dotnet restore Ananuri.SlotEngine.sln
dotnet build Ananuri.SlotEngine.sln -c Release --no-restore
dotnet test Ananuri.SlotEngine.sln -c Release --no-build --no-restore
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- tutorial
```

The tutorial charges 100 units and pays 500, 300, and 600 across one paid spin and two
free spins: 1,400 gross payout in total. It then verifies recorded replay. These are
scripted teaching inputs, not an RTP estimate.

Run the complete local verification and packaging sequence from PowerShell:

```powershell
./scripts/verify.ps1
```

The script checks formatting, tests, exact and seeded sample results, local documentation
links, and package contents. It also compiles and runs both documented integration
programs against the actual generated NuGet package. It stops at the first failure.
It also compiles host fragments, exercises the game-creation tutorial, and checks
the documented current-game settings and calibration fingerprint.
Only PowerShell and .NET are required.

In Visual Studio, open the existing solution, build it, and run Test Explorer.
Use the terminal command above
for the tutorial so its working directory and arguments are explicit.

## What is in the solution?

- **Ananuri.SlotEngine:** the reusable library. It has no external NuGet dependencies.
- **Ananuri.SlotEngine.Simulator:** console tooling that calls the library.
- **Ananuri.SlotEngine.Tests:** xUnit tests with mathematical expectations and regression cases.

One `SlotEngine` implements `ISlotEngine` for every supported game. A `GameDefinition`
selects win evaluation, cascades, features, multipliers, and an optional payout cap.
Plain payline games use the same request and result types as games with free spins.
The host supplies random allocations and owns players, balances, storage, concurrency,
and settlement. The library provides neither a game UI nor a complete server.

## Reference and development

- [Architecture](docs/architecture.md): folders, components, and execution flow.
- [Engine rules](docs/engine-rules.md): precise feature behavior and extension contracts.
- [Plain-payline configuration](docs/plain-payline-rules.md): one-board games using the same engine.
- [Developer guide](docs/implementation-guide.md): modifying and extending the current solution.
- [Verification status](docs/verification.md): checks performed and their limits.

Examples: [PaylineGame.json](samples/PaylineGame.json) (one board, no features or cap),
[FiveReelGame.json](samples/FiveReelGame.json) (five-reel reference game),
[FirstGame.json](samples/FirstGame.json) (the worked tutorial),
[TutorialExample.cs](samples/TutorialExample.cs),
[CascadingGame.json](samples/CascadingGame.json),
[CollectedSymbolsGame.json](samples/CollectedSymbolsGame.json), and
[fixed-payline reference vectors](docs/reference-vectors.json).

CascadingGame is experimentally calibrated toward its documented payout targets.
The other samples are untuned teaching/test fixtures; none is independently certified.
Sample game definitions live in JSON. The simulator loads these files, and tests use
the same files alongside independently calculated expected results. `TutorialExample.cs`
demonstrates execution, checkpointing, and replay; it does not define game rules.
Exact replay requires retained game and engine artifacts as well as recorded inputs.
