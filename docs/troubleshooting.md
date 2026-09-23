# Troubleshooting

Start with the first failing command. Later errors can be consequences of an earlier
failed restore or build. Run commands from the folder containing the solution unless
you provide absolute paths. The [setup guide](start-here.md) has the normal sequence.

## Tools and files

| Symptom | Likely cause | What to do |
| --- | --- | --- |
| `dotnet` is not recognized | SDK missing or not on PATH | Install the .NET SDK selected in `global.json`, open a new terminal, and check `dotnet --version`. A runtime-only installation cannot build. |
| A compatible SDK was not found | Exact SDK 10.0.400 is unavailable | Run `dotnet --list-sdks`; install the pinned SDK. Changing `global.json` is a deliberate toolchain change, not the default troubleshooting step. |
| NuGet restore fails (`NU1301`, network, proxy, or signature errors) | NuGet cannot be reached or a network policy blocks it | Check access to your configured package source/proxy. In an agent sandbox, grant the required network permission. Restore must succeed before relying on `--no-restore`. |
| Solution/project file does not exist | Wrong current folder or mistyped path | Check `Get-Location` and `Test-Path ./Ananuri.SlotEngine.sln`. Quote paths containing spaces. |
| `--no-build` cannot find the executable | Release build has not completed | Run `dotnet build Ananuri.SlotEngine.sln -c Release` first. Debug and Release outputs are separate. |
| Game JSON cannot be found | Explicit relative path resolved against another folder, or missing copied sample | Pass an absolute path for your own JSON. Without an argument, `tutorial` loads `Samples/FirstGame.json` beside the simulator executable (`AppContext.BaseDirectory`); rebuild to restore copied samples. |
| Editing the default sample seems ineffective | Simulator is using the JSON copy in its output folder | Rebuild to refresh the copy, or pass the edited JSON file explicitly to `game-demo` or `game-simulate`. |
| PowerShell refuses to execute the verification script | Local execution/signing policy | Run the individual documented .NET commands, or follow your organization's script-signing policy. No policy change is needed to use the engine. |

## Configuration errors

| Symptom | Check |
| --- | --- |
| `JsonException` mentioning missing/unknown members | Exact camelCase names, required nested fields, enum strings, and valid JSON without comments or trailing commas. |
| Duplicate package property | Each object may contain a property name only once. Remove the duplicate rather than depending on overwrite order. |
| Unsupported schema/profile/strategy | Use schema 1, profile `slot-engine-v1`, and a listed strategy ID. Set `cascadePolicy` explicitly. Adding a new JSON name does not implement a new mechanic. |
| Missing or incompatible refill configuration | `falling-symbols-v1` requires paid refill weights; `none-v1` rejects paid/free refill weights and `allowScatterRefills: true`. |
| Invalid round cap | Omit or use null for no cap, or specify a positive `roundWinLimitMultiplier`; zero does not disable the cap. |
| Unknown or undeclared symbol | Declare the ID in `symbols` and check every reel, weight, award, priority, and feature reference. |
| Invalid payline | Exactly one row per reel, rows within the board, unique ID, and unique path. Indices start at zero. |
| Stake cannot divide across lines | Every allowed total stake must divide evenly by active line count. |
| Invalid or decreasing paytable/threshold | Remove duplicate keys and make larger match/count thresholds nondecreasing in awards. |
| Scatter triggers require a feature | Set a positive `maximumAwardedFreeSpins`, or set requested free spins to zero for money-only scatters. |
| Scatter refill opt-in error | Either remove scatter weights or enable `allowScatterRefills` and intentionally choose scatter timing. |
| Incompatible symbol roles | Scatters and collection symbols cannot have ordinary paytable awards or participate in wild substitution. |
| Null collection value / weight / reel | Array entries must be real valid objects/arrays, not null placeholders. |
| Payout storage overflow bound | Reduce allowed stakes or multiplier bounds. Raw grid awards must fit even if the round cap is lower. |

See [the complete configuration reference](configuration-reference.md) for permitted
fields, defaults, and limits. Many valid packages can still contain unreachable awards;
validation is not a full mathematical analysis of the strips.

## Execution and saved state

| Symptom | Explanation / action |
| --- | --- |
| Unsupported calculation stake | Use one of the package's allowed stakes. Free spins must retain the original triggering stake. |
| Draw outside requested range | The source must return an integer from zero through bound minus one. Fix the adapter; do not clamp or redraw a funded outcome. |
| `FixedReelStops` rejects a draw | It supplies initial stops only. Use a general draw source for refill requests; for free spins, construct it with `SpinMode.Free`. |
| Missing/mismatched replay draw | Preserve evaluation ID, ordinal, purpose, bound, and the exact matching game/engine. Do not replace missing historical draws with fresh ones. |
| Missing required state field | Store complete state via `SpinStateSerializer`. Recover incomplete historical data from authoritative records; do not assume zero/defaults. |
| Bonus/continuation package or engine mismatch | Resume using the historical package/artifact. Do not edit identity fields just to bypass the check. |
| Invalid bonus counters | Use the returned `NextBonus`; guard concurrent consumption in the host. Do not manually decrement it before calling the engine. |
| `WorkBudget` and null `NextBonus` | The spin is unfinished. Use `Continuation` and `Resume`; wait for completion before interpreting the next bonus. |
| Duplicate stake/payout in a host | `ChargedStakeUnits` and payout totals repeat cumulatively across resumes. Persist and settle idempotently; see the result reference. |
| Custom policy uses an undeclared symbol or feature | Check its immutable symbol-role declarations and free-spin capabilities against actual behavior in both modes. See [component contracts](implementation-guide.md). |
| Evaluator exceeds its declared payout bound | Check the evaluator's rules and conservative bound for that stake. Ordinary bounds cover the sum of base awards per grid; scatter bounds are multiples of total stake. Do not suppress the error or replace the outcome with a loss. |
| Custom cascade returns inconsistent evidence | Compare removals, surviving IDs/symbols, fresh contiguous IDs, movements, and arrivals against both boards. Test the policy's contract before use. |
| Simulator reaches a round work limit | Inspect the package for endless wins or bonuses. The default limits are 10,000 grids and 1,000 spins across a paid round; `--max-grids-per-round` and `--max-spins-per-round` set positive limits. The command fails with an unfinished-round error instead of reporting truncated statistics. Ctrl+C cancels. |
| Exact enumeration exceeds its limit or rejects the package | Enumeration derives all initial stop combinations from the JSON strips and defaults to at most 1,000,000 combinations. Use `--max-combinations` deliberately, and use `game-simulate` for games with cascades or free spins. |

## Results that may look surprising but are intentional

An all-wild line may pay an ordinary target instead of the wild's own award. A longer
qualifying target can lose to a higher-paying shorter target. Refilled scatters do not
trigger under initial-grid-only timing. A final free spin can retrigger. A cap can end
the round before pending free spins start. These are specified behaviors in
[cascading rules](engine-rules.md), not necessarily calculation defects.

For a reproducible bug report, retain the package, engine version/artifact, request,
continuation or bonus if applicable, and recorded draws. Describe the expected board
and arithmetic as well as the actual result. A seed alone is not sufficient evidence
for portable historical replay.
