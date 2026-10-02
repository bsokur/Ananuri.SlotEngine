# Ananuri.SlotEngine

A .NET library that calculates slot-game boards, awards, payouts, cascades, and
free-spin state from a game definition and externally supplied random draws.
The repository includes a console simulator, executable examples, and regression tests.

One `SlotEngine` implements `ISlotEngine` for both plain-payline and featured games.
The host owns players, balances, random allocation, persistence, concurrency, and
settlement. The library is a calculator, not a game UI or server.

## Start here

Use the .NET SDK **10.0.400** pinned in [global.json](global.json). Open
`Ananuri.SlotEngine.sln` in Visual Studio, or run these commands from the repository:

```powershell
dotnet restore Ananuri.SlotEngine.sln
dotnet build Ananuri.SlotEngine.sln -c Release --no-restore
dotnet test Ananuri.SlotEngine.sln -c Release --no-build --no-restore
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- tutorial
```

The tutorial pays 500, 300, and 600 units across one paid spin and two free spins,
then verifies replay. These scripted results are not an RTP estimate.

## Essential guides

| Guide | Purpose |
| --- | --- |
| [Start here](docs/start-here.md) | Setup, terminology, and the first round |
| [Integration](docs/integration.md) | Runnable host examples, draws, persistence, and settlement |
| [Configuration](docs/configuration-reference.md) | JSON fields, defaults, and restrictions |
| [Results and state](docs/results.md) | Payout evidence, checkpoints, and bonus state |
| [Engine rules](docs/engine-rules.md) | Calculation order, feature semantics, and policy contracts |
| [Architecture](docs/architecture.md) | Component responsibilities, extension points, and development checks |
| [Source walkthrough](docs/source-walkthrough.md) | Debugger path through a complete round |
| [Simulation and math](docs/simulation-and-math.md) | Simulation commands, current game settings, RTP, and volatility |

## Verify changes

```powershell
./scripts/verify.ps1
```

This runs the Release build, formatting, tests, deterministic simulations/replay,
documentation-link checks, and NuGet consumer examples. PowerShell and .NET are the
only required runtimes. No package is published.

## Examples and compatibility

[FirstGame.json](samples/FirstGame.json) and [TutorialExample.cs](samples/TutorialExample.cs)
demonstrate checkpoints, free spins, and replay. [PaylineGame.json](samples/PaylineGame.json)
is the small exact-enumeration fixture; [FiveReelGame.json](samples/FiveReelGame.json),
[CascadingGame.json](samples/CascadingGame.json), and
[CollectedSymbolsGame.json](samples/CollectedSymbolsGame.json) exercise other configurations.
[CustomMultiplierExample.cs](samples/CustomMultiplierExample.cs) demonstrates policy extension.
[Reference vectors](docs/reference-vectors.json) provide independently expected plain-payline outcomes.

Library version: **0.1.0**. JSON schema: **1**. Rule profile: `slot-engine-v1`.
The package ships these guides and samples; source-code links require the checkout.
Exact replay requires retained game/engine artifacts and recorded inputs.
Samples are experimental or teaching fixtures, not independently certified games.
