# Verification status

Initial release: 2026-09-21, library version 0.1.0, package schema 1.
CascadingGame payout split and paid scatter awards recalibrated on 2026-09-23, math version experimental-3.

## Repeatable command

Run from PowerShell:

```powershell
./scripts/verify.ps1
```

The script uses the .NET SDK 10.0.400 pinned in global.json. It restores packages,
builds the full solution in Release, verifies formatting, runs xUnit, and asserts
the expected tutorial, exact enumeration, replay, and seeded simulation results.
It checks local documentation links and heading targets, creates the NuGet package,
checks its contents, and runs both complete integration programs against the actual
package in temporary console hosts. The complete documented JSON package is also
loaded and played. It stops on failure, removes its temporary consumer directories,
and restores the caller's working directory.
No additional scripting runtime or remote CI provider is required.

## What is checked

- Known fixed-payline grid and payout examples are loaded from
  [reference-vectors.json](reference-vectors.json) directly by xUnit.
- Tiny-game enumeration independently expects 54 losses, two 400-unit payouts,
  and eight 600-unit payouts across 64 equally weighted stop combinations.
  At stake 100 each, gross payout is 5,600 and exact RTP is 87.5%.
- An independent capped cascading fixture has payout probabilities 3/4, 3/16,
  3/64, and 1/64 for 0x, 1x, 2x, and 3x stake.
- Tests cover wild selection, gravity, weighted intervals, scatters, bonus
  lifecycle, multiplier timing/persistence, limits, invalid draws, package loading,
  state serialization, faulty custom transitions, concurrency, and resumed replay.
- Custom-policy cases verify declared symbol roles, free-spin capabilities, evaluator-specific
  payout bounds at every stake, runtime contract violations, and no-cascade identity transitions.
- [The teaching example](first-round.md) is compiled into the simulator and exercised by tests. It expects
  spin payouts 500, 300, and 600; final round payout 1,400; and 18 replayed draws.
- Scripted simulator runs compare complete serialized evaluation results against replay.
- The script checks the exact enumeration distribution, all tutorial payouts and draw
  count, each scripted run's payout and replay counts, and four seeded 10,000-round simulation
  totals, RTP, free-spin counts, and refill counts. Seeded checks are regression
  expectations, not independent proofs of the larger samples' mathematics.
- Deliberately exhausted grid and spin budgets must exit with an unfinished-round
  error and must not publish completed-run statistics.
- Documentation checks read the current Markdown directly. Both C# integration
  programs build against the newly produced `.nupkg`, using its tutorial source/data,
  a local-only package source and an isolated package cache. The consumer restore
  therefore cannot silently reuse an earlier build of version 0.1.0.
- Both C# host fragments in engine-rules.md compile with parameter stubs against
  the same package. They are compilation checks; the complete integration programs
  provide execution checks.
- The game-creation guide's copy commands run in a temporary directory. Its edited
  package must pay 1,500 units and pass replay, without adding a shipped sample.
- The current-game rules table must match CascadingGame.json, and its documented
  calibration fingerprint must match the engine's compiled game identity. This
  detects configuration drift; it does not rerun the larger calibration.
- Package checks require the library, XML IntelliSense documentation, current guides,
  five JSON samples and tutorial source, and reject historical folders or removed
  sample-definition code.

## Recorded results

Verified with .NET SDK 10.0.400 and runtime 10.0.11:

- Release build completed with zero warnings and zero errors; all tests passed.
- The simulator's `demo` command paid 500 units. The tutorial paid 500, 300, and 600 units, finishing
  at 1,400 with an exact 18-draw replay.
- Exact tiny-game enumeration covered all 64 stop combinations, paid 5,600 units,
  and returned 87.5% RTP.
- All four scripted package runs passed complete-result replay, and their 10,000-round
  simulations with seed 12345 completed successfully. Results are below.
- Both complete C# integration programs compiled and ran in a separate console host.
  The plain program printed `Payout: 500`. The complete JSON example in the field
  reference also paid 500 and passed replay.
- The 2026-09-23 documentation review also compiled both engine-rule fragments,
  verified the game-creation exercise's 1,500-unit round and 18-draw replay, and
  matched the current rules table and calibration fingerprint to CascadingGame.
- Local documentation links resolved. The 0.1.0 NuGet package was built, and its
  archive paths, documentation, and included samples were inspected.
- Default demo, tutorial, enumeration, and game-demo commands also ran from outside
  the solution folder, loading the JSON samples copied beside the simulator.

Each simulation charged a total of 1,000,000 units:

| Package | Base payout | Bonus payout | Observed RTP | Free spins | Refill transitions |
| --- | ---: | ---: | ---: | ---: | ---: |
| FiveReelGame.json | 135,500 | 0 | 13.55% | 0 | 0 |
| PaylineGame.json | 862,000 | 0 | 86.2% | 0 | 0 |
| CascadingGame.json | 378,050 | 547,675 | 92.5725% | 11,175 | 4,142 |
| CollectedSymbolsGame.json | 508,500 | 215,500 | 72.4% | 833 | 1,488 |

These small runs are deterministic regression observations, not exact theoretical RTP
values. CascadingGame is calibrated toward 96% using the larger calibration linked below;
the other fixtures remain untuned. Short sessions can differ substantially from the target.

## CascadingGame RTP calibration

The current game's checked rules, 96% RTP and 40/60 payout-share targets, measured
10-million-round results, volatility, and reproduction commands are maintained in
[simulation and game math](simulation-and-math.md). The longer calibration remains
separate from quick regression verification. Recalibrate after probability or award
changes; previous measurements apply only to their recorded fingerprint and engine.

## Limits of verification

Passing tests and replay verifies the covered contracts and examples. It does not
prove every possible custom component, reachable maximum payout, or a commercial
game's exact mathematics. Production random allocation, durable settlement, host
recovery, and deployment requirements belong to the surrounding application and
need their own verification. See [integration](integration.md).
