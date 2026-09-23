# Follow one complete round

Run `tutorial` as shown in [Start here](start-here.md). Its configuration is
[FirstGame.json](../samples/FirstGame.json). This page follows that exact execution.

## The rules in this small example

There are three reels, two visible rows, and one payline selecting row 1 on every reel.
The package selects `cascadePolicy: "falling-symbols-v1"`; the same engine can also
run one-board games with `"none-v1"`.
Symbol `1` is A, `2` is B, `3` is C, and `8` is scatter S. These letters are explanatory
labels; the package stores integers. Three A symbols on the line pay 5 times line stake;
three B symbols pay 3 times line stake. C has no award. Three scatters on the initial
paid grid award two free spins. Scatters have no monetary award here.

The stake is 100 units. With one line, the line stake is also 100. Replacements are
always C because C is the sole entry in each refill table. The complete round has a
cap of `100 × 50 = 5,000` units.

The tutorial draw source always returns zero, choosing the first stop and first weighted
interval. No probability claim should be inferred from the following outcome.

## 1. Generate the paid board

Each paid reel is `[S, A, C]`. Stop 0 puts S at row 0 and A at row 1:

```text
             reel 0   reel 1   reel 2
row 0           S        S        S
row 1           A        A        A    <- the one active payline
```

Instances are assigned reel by reel: reel 0 has IDs 0 and 1, reel 1 has 2 and 3,
and reel 2 has 4 and 5. These IDs identify cells even after their positions change.

## 2. Evaluate grid 0

Scatter counting finds three S symbols and requests two free spins. Those spins wait
until the paid spin has finished all its cascades. The payline begins at reel 0 and
matches A across all three reels.

```text
line stake            = 100 / 1 = 100
ordinary base award   = 100 × 5 = 500
paid feature factor   = 1
raw grid payout       = 500 × 1 + 0 scatter payout = 500
remaining round cap   = 5,000
payable grid payout   = 500
```

The three A positions are the selected winning cells. The S cells did not contribute
to that ordinary award and are not removed.

## 3. Remove, fall, and refill

Remove the A instances 1, 3, and 5. The surviving S instances fall from row 0 to row 1.
Three new C instances, IDs 6, 7, and 8, fill row 0:

```text
             reel 0   reel 1   reel 2
row 0           C        C        C    <- new arrivals
row 1           S        S        S    <- original scatter instances
```

The transition records removals, each S movement, and each C arrival. Grid 1 has no
ordinary win. Scatter timing is initial-grid-only, so falling S symbols do not trigger
again. No further removals exist, and the paid spin completes with a 500-unit payout.

The tutorial uses a one-grid work budget. It actually serializes a continuation after
grid 0, reloads it, and calls `Resume` for grid 1. No new wager is made at that boundary.

## 4. Activate and complete the free spins

The completed paid spin returns `NextBonus` with two remaining spins and multiplier 1.
Free reels are `[C, B, C]`, so stop 0 produces:

```text
row 0           C        C        C
row 1           B        B        B
```

In each free spin, B pays, B is removed, and C falls onto the payline. The resulting
all-C board loses. The multiplier increases once after the paying grid, and persists
between free spins. A losing grid does not increase it.

| Evaluation | Charged stake | B/A base award | Applied factor | Spin payout | Round payout | Next free-spin state |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Paid | 100 | A: 500 | 1 | 500 | 500 | 2 spins, factor 1 |
| Free 1 | 0 | B: 300 | 1 | 300 | 800 | 1 spin, factor 2 |
| Free 2 | 0 | B: 300 | 2 | 600 | 1,400 | None; bonus finished |

The factor advances after awarding the current grid, so Free 1 pays at 1x, not 2x.
There is no free-spin retrigger table in this package. The 5,000-unit cap is never reached.

## 5. Read the totals correctly

The completed spin payouts sum to `500 + 300 + 600 = 1,400`. The last `RoundPayoutUnits`
is also 1,400. Do not add round totals `500 + 800 + 1,400`; they are cumulative.
Only 100 units were charged. The gross payout is 1,400; a hypothetical balance change
would be `1,400 - 100 = 1,300`, handled by a host rather than by this library.

There are six draw requests per spin: three initial stops and three refills. Across
three evaluations there are 18 recorded requests. Replay reproduces the complete
results using those requests, including the same IDs, payouts, and bonus transitions.

The tutorial IDs are fixed for repeatable local teaching. A funded host must generate
unique evaluation IDs and durably associate draws and settlement with them.

Next: [change a rule yourself](create-a-game.md), or [interpret every result field](results.md).
