# Simulation and game math

Run these PowerShell commands from the solution folder with the SDK pinned in
[global.json](../global.json). The simulator completes one paid spin and its entire
bonus before recording an observation. Scripted simulator examples are excluded
from RTP measurement.

## Run a simulation

Build once, then run a quick regression sample or a larger statistical sample:

```powershell
dotnet build tools/Ananuri.SlotEngine.Simulator -c Release
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- game-simulate 10000 12345 samples/CascadingGame.json
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- game-simulate 1000000 601 samples/CascadingGame.json
```

Arguments are round count, integer seed, and package path. An explicit path reads
the current JSON without rebuilding. Omitting it loads the bundled CascadingGame
copy, which needs a rebuild after edits. The simulator uses the smallest allowed stake
(100 in this sample); there is no stake command-line option. To simulate another
stake, use a separate experimental package with `allowedStakes: [200]`, for example.
The engine sorts stakes, so reordering the existing list does not change the selected stake.

The 10,000-round seed-12345 regression measures 92.5725% RTP. This short run is useful
for detecting changes, not deciding whether the game meets its long-run target.
Use multiple larger runs and reserve fresh seeds for validation after tuning.

Optional `--max-grids-per-round N` and `--max-spins-per-round N` bound execution work.
Exhausting a budget or pressing Ctrl+C aborts the run; do not treat partial output
as completed simulation evidence. See [results](results.md#simulator-statistics)
for every reported statistic.

## Current rules

[CascadingGame.json](../samples/CascadingGame.json) is the source of truth. Verification
compares the following table with that file and checks its compiled fingerprint.
Edit the package first, update this table, then recalibrate when probabilities or
awards change. Other guides refer here for game-specific settings.

<!-- cascading-rules:start -->
| Setting | Current value |
| --- | --- |
| Game ID | `cascading-wilds-example` |
| Math version | `experimental-3` |
| Board | 5 reels × 4 rows |
| Payline rows (left to right) | 0/0/0/0/0; 1/1/1/1/1; 2/2/2/2/2; 3/3/3/3/3 |
| Allowed stakes | 100, 200, 500 |
| Wild symbols / substitution targets | 7 / 1, 2, 3, 4, 5, 6 |
| Symbol 1: 3/4/5 matches, line-stake multiples | 6/12/23 |
| Symbol 2: 3/4/5 matches, line-stake multiples | 5/9/18 |
| Symbol 3: 3/4/5 matches, line-stake multiples | 3/7/14 |
| Symbol 4: 3/4/5 matches, line-stake multiples | 3/5/9 |
| Symbol 5: 3/4/5 matches, line-stake multiples | 2/5/9 |
| Symbol 6: 3/4/5 matches, line-stake multiples | 2/5/9 |
| Scatter symbol / timing | 8 / InitialGrid |
| Paid scatters: minimum count → spins + total-stake cash | 3 → 7 + 1x; 4 → 11 + 2x; 5 → 13 + 5x |
| Free scatters: minimum count → spins + total-stake cash | 3 → 3 + 0x |
| Multiply scatter cash | False |
| Allow scatter refills | False |
| Maximum lifetime awarded free spins | 100 |
| Paid multiplier | constant-v1: 1x |
| Bonus multiplier | paying-cascade-v1: start 1x, increment 1, maximum 3x, persistence Bonus, include initial grid True |
| Gross round cap, total-stake multiple | 5000x |
<!-- cascading-rules:end -->

The total stake is divided equally among the paylines. Ordinary awards match from
the leftmost reel, with wild substitution. Winning cells are removed, survivors
fall, and weighted replacements arrive until the spin ends. The JSON contains the
exact reel strips and refill weights; free spins reuse them in this sample.

Only the highest qualifying scatter tier pays, once on the initial board. Paid
scatter cash is guaranteed for a qualifying trigger even with no ordinary win;
free-spin retriggers pay no scatter cash. All awards remain subject to the gross
round cap. The bonus multiplier applies its current value before increasing after
an ordinary paying grid, and persists between free spins.

## Targets and measured calibration

The experimental target is **96% RTP**, with **40% of total payout from paid spins**
and **60% from free spins**. These correspond to **38.4 and 57.6 percentage points
of RTP**. Paid-spin scatter cash belongs to the base contribution. These are design
targets, not values the engine enforces by adjusting individual outcomes.

Calibration identity (SDK 10.0.400, runtime 10.0.11):

`23FDD2BF8D940DD6865F0069365F63717A6E9A419AD4904BE9FD5314FC090935`

On 2026-09-23, ten held-out seeds (601–610) each sampled 1,000,000 complete paid
rounds at stake 100. Runs used to choose the settings were excluded.

| Seed | Observed RTP | Base share of payout |
| ---: | ---: | ---: |
| 601 | 95.908475% | 39.756706% |
| 602 | 95.658300% | 39.973269% |
| 603 | 95.798100% | 39.908542% |
| 604 | 96.548025% | 39.654851% |
| 605 | 96.196100% | 39.811255% |
| 606 | 95.888900% | 39.858107% |
| 607 | 96.151900% | 39.958285% |
| 608 | 96.450100% | 39.831918% |
| 609 | 96.291050% | 39.850043% |
| 610 | 96.513325% | 39.816393% |

Total charged stake was **1,000,000,000**, base payout **383,040,500**, and bonus
payout **578,363,775**. Combined RTP was **96.1404275%**: **38.30405% base** plus
**57.8363775% bonus**. Shares of payout were **39.8417721% base / 60.1582279% bonus**.
The approximate pooled 95% normal RTP interval was **95.9468%–96.3341%**.
No round hit the configured cap. Absence of cap hits does not prove the cap unreachable.
The other allowed stakes scale payouts and limits proportionally for identical draws.

## Volatility and hit frequency

For that same 10-million-round sample:

| Metric | Measured value |
| --- | ---: |
| Complete-round payout standard deviation | approximately 3.1248× stake |
| Complete-round payout sample variance | approximately 9.7644 stake² |
| Round hit frequency (gross payout greater than zero) | 26.20143% |
| Zero-payout rounds | 73.79857% |
| Bonus-trigger frequency per paid round | 10.36754% |

Volatility describes the spread of complete-round returns, including the entire
bonus. It is not determined by RTP or the base/bonus split alone. A hit can pay less
than the stake; hit frequency is not the probability of making a profit. Subtracting
one stake from each normalized gross return gives net returns with the same standard
deviation. Individual free spins are correlated and are not separate observations
for this calculation. This project does not define low/medium/high rating thresholds.

These estimates support the target but are neither exact theoretical mathematics
nor independent certification. Rare large outcomes can affect both volatility and
RTP estimates; a normal confidence interval does not establish tail coverage.

## Save and combine validation runs

Use a fresh output directory for each calibration. This copies the exact package,
records the environment, and stops if any process fails:

```powershell
$runDirectory = Join-Path ./artifacts ('calibration-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runDirectory | Out-Null
Copy-Item ./samples/CascadingGame.json (Join-Path $runDirectory 'game.json')
dotnet --info | Set-Content (Join-Path $runDirectory 'dotnet-info.txt')
if ($LASTEXITCODE -ne 0) { throw 'Could not record the .NET environment.' }
foreach ($seed in 601..610) {
    dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- game-simulate 1000000 $seed (Join-Path $runDirectory 'game.json') 2>&1 |
        Tee-Object -FilePath (Join-Path $runDirectory "seed-$seed.txt")
    if ($LASTEXITCODE -ne 0) { throw "Simulation failed for seed $seed." }
}
```

Build first using the earlier command. Retain the engine/simulator binaries and
source revision with these files; seeds alone are not a portable cross-runtime
replay contract. Each log includes the compiled game fingerprint, math version,
engine/rule versions, seed, runtime, round count, and statistics. Only combine
successful runs of the same game, engine, runtime, and stake, with distinct seeds.
Do not count repeated runs of the same seed as additional independent evidence.

Let `S` be the sum of charged stakes, `B` base payouts, and `F` bonus payouts:

- RTP percent = `100 × (B + F) / S`.
- Base/bonus RTP contributions = `100 × B / S` and `100 × F / S`.
- Base/bonus payout shares = `100 × B / (B + F)` and `100 × F / (B + F)` when payout is positive.

For run `i`, let `n_i` be its rounds, `m_i` its gross mean in stake multiples
(`RTP percent / 100`), and `s_i` its reported standard deviation. Pool using:

```text
N = sum(n_i)
m = sum(n_i * m_i) / N
variance = sum((n_i - 1) * s_i² + n_i * (m_i - m)²) / (N - 1)
standard deviation = sqrt(variance)
95% RTP interval = 100 * (m ± 1.96 * sqrt(variance / N))
```

Do not average run standard deviations or confidence bounds. The between-run term
is necessary. Printed standard deviations are rounded, so pooling logs is approximate.
Pool hit and bonus counts over total rounds; weight reported percentages by round
count if counts are unavailable. Longer validation is separate from
[quick verification](verification.md), which checks regressions rather than recalibrating.
