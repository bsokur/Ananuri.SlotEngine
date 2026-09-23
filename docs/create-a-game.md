# Create your first game configuration

First run [the tutorial](start-here.md) and read [the worked round](first-round.md).
This exercise changes data only; the library source stays the same.

For a game with only one board per spin, start from
[PaylineGame.json](../samples/PaylineGame.json) instead. It sets
`cascadePolicy: "none-v1"` and omits refill and feature settings. The exercise below
uses the tutorial package so you can see how several independent components interact.

## 1. Make a separate package

From the solution folder in PowerShell:

```powershell
New-Item -ItemType Directory -Path ./experiments -Force | Out-Null
Copy-Item ./samples/FirstGame.json ./experiments/MyGame.json
```

The `samples` directory contains the five shipped packages. Keep experiments outside it
so they are not included in the NuGet package.

Open `experiments/MyGame.json` in a text editor. Change `gameId` to `"my-first-game"`
and `mathVersion` to `"experiment-1"`. JSON property spelling matters. Do not insert
comments into the file. Use [the field reference](configuration-reference.md) when
you are unsure what a setting means.

## 2. Change one payout and predict the result

In `paytable`, change symbol 1's multiplier from 5 to 6. Leave symbol 2's entry alone.
Run:

```powershell
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- tutorial experiments/MyGame.json
```

The paid spin should now pay `100 × 6 = 600`. The free spins should still pay 300 and
600. Expected final round payout: 1,500. Charged stake remains 100. Replay should pass.
The altered game ID/math version and award change produce a different fingerprint.

If you only edited JSON, rebuilding C# is unnecessary. The explicit file path causes
the package to be read afresh each run. Default commands use JSON copied into the
simulator's output folder by the build; rebuild to refresh those copies, or use an
explicit file path while tuning.

## 3. Try another controlled experiment

Return symbol 1's multiplier to 5, then choose one change at a time:

| Change | Expected scripted consequence |
| --- | --- |
| Set `maximumAwardedFreeSpins` to 1 | The requested two spins are limited to one. Paid payout 500, free payout 300, round total 800. |
| Set bonus `persistence` to `"Spin"` | Both free spins begin at 1x: payouts 300 and 300; round total 1,100. |
| Set `roundWinLimitMultiplier` to 6 | The paid spin pays 500. The first free spin has raw payout 300 but only 100 allowance, so it pays 100 and ends the round. Total 600. |
| Change `allowedStakes` to `[200]` | The tutorial chooses the smallest allowed stake. All monetary payouts double; free-spin counts are unchanged. |

Restore the original value before trying the next row. These predictions use the
tutorial's all-zero choices, not arbitrary random outcomes. The
[result reference](results.md) explains raw amounts and cap adjustments.

## 4. Add a symbol or payline deliberately

To introduce a paying symbol, declare its unique ID in `symbols`, make it appear in
appropriate strips or refill tables, and add its paytable entries. Merely declaring
an ID does not make it appear. Merely putting it on a strip does not make it pay.

To add a payline, give it a new ID and a different row path of exactly reel-count
length. For example, `[0,0,0]` adds the top horizontal line to the tutorial. With two
lines, stake 100 becomes 50 per line, so the previous payout predictions no longer apply.
The allowed stakes must divide evenly by the new number of lines.

For wilds or collection symbols, use the worked configuration objects in
[the reference](configuration-reference.md). Check role restrictions before combining
mechanics: scatters and collection symbols are not ordinary paying targets.

## 5. Sample complete rounds

```powershell
dotnet run --project tools/Ananuri.SlotEngine.Simulator -c Release --no-build -- game-simulate 10000 12345 experiments/MyGame.json
```

This command uses uniform initial stops and weighted refills, unlike `tutorial`.
It completes each paid spin and all its bonuses before recording round statistics.
The output includes paid stakes, base/bonus payouts, RTP, hit frequency, cap hits,
bonus lengths, and payout bands. See [interpreting statistics](results.md#simulator-statistics).
Another seed may give different results; a short simulation is not the exact game math.

## 6. Make the change maintainable

Before promoting an experiment, write xUnit cases with independently calculated
expected boards and payouts, including losses, feature boundaries, and caps. Keep the
exact package used for those cases. Run `./scripts/verify.ps1` after code changes;
its existing samples do not automatically validate every new file you create.

For a released game, retain immutable copies of its package and engine artifact.
Do not reinterpret an unfinished spin or active bonus with the edited package. The
fingerprint/version checks are designed to reject that mismatch. Future feature
development belongs in [the developer guide](implementation-guide.md).
