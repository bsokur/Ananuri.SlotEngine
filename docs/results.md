# Read results, checkpoints, and bonus state

Start with [the worked round](first-round.md). The following field names are the C#
names exposed by the unified engine API. Default state serialization preserves these names;
game-package JSON uses different camelCase transport fields.

## The top-level `SpinEvaluation`

| Field | Meaning / how to use it |
| --- | --- |
| `EvaluationId` | Identifies this single paid or free spin. Unchanged across resume calls. |
| `GameId`, `MathVersion` | Labels from the game definition. |
| `GameFingerprint` | Hash of compiled configuration; retain with historical state. |
| `EngineVersion`, `RulesVersion` | Engine artifact label and rule family. Exact replay also needs the artifacts. |
| `Mode` | `Paid` or `Free`, determined by whether the input request has a bonus. |
| `ChargedStakeUnits` | Original wager description: positive for paid spins, zero for free spins. Repeated on resume; not an instruction to debit again. |
| `CalculationStakeUnits` | Basis for awards and round cap, retained from the paid spin during a bonus. |
| `StartingBonus` | Bonus state supplied for this free spin, or null for a paid spin. |
| `InitialStops` | One stop per reel for this spin's first board. These stay the same through cascades. |
| `Steps` | Evidence for grids processed in this call only. Append across resumes if retaining the whole trace. |
| `TotalPayoutUnits` | Cumulative payable payout for this spin. Take the completed value once; do not sum values from resume calls. |
| `RoundPayoutUnits` | Cumulative payable payout for the paid spin and associated bonus so far. |
| `GrantedFreeSpins` | Actual newly granted spins at completion after the lifetime allowance; may be smaller than scatter requests. Zero at work-budget boundaries. |
| `NextBonus` | State for the next free spin after this spin completes. Null on a work-budget response as well as when no next bonus exists. Check `IsComplete` first. |
| `CompletionReason` | `NoMoreWins`, `WinLimit`, or `WorkBudget`. |
| `Continuation` | Non-null when computation paused; pass to `Resume`, not `Evaluate`. |
| `IsComplete` | True when there is no continuation. |

`NoMoreWins` means no further cascade is selected; with a `NoCascades` policy it can
finish a paying board. `WinLimit` ends the whole round and discards pending bonus play.
`WorkBudget` means pause and resume, not a terminal game result.

These types apply to plain-payline games too. With `NoCascades`, `Steps` contains one
grid when the spin completes. With `NoWinLimit`, raw and payable grid payouts are
equal, `CapAdjustmentUnits` is zero, and completion is not caused by a win cap.

For the tutorial's completed paid spin:

```text
Mode                 Paid
ChargedStakeUnits     100
CalculationStakeUnits 100
TotalPayoutUnits      500
RoundPayoutUnits      500
GrantedFreeSpins      2
NextBonus.RemainingSpins 2
CompletionReason     NoMoreWins
Continuation         null
IsComplete           true
```

## Each `CascadeStep`

| Field | Meaning |
| --- | --- |
| `GridIndex` | Zero-based grid number within the spin. |
| `Grid` | Board before this step's removals/refill; `Cells` are reel-major `SymbolInstance` records with `Id` and `Symbol`. |
| `Awards` | Selected ordinary awards, each wrapped with its applied factor and raw monetary payout. |
| `Scatter` | Count, requested free spins, base monetary payout, counted positions, and counted instance IDs. |
| `ScatterAppliedMultiplier` | Factor applied to scatter money; 1 unless configured otherwise. |
| `MultiplierBefore` | Strategy state entering the grid. |
| `AppliedMultiplier` | Factor used for this grid's ordinary awards. |
| `MultiplierAfter` | State after this grid; stays at the prior state when the cap ends the round. |
| `Collections` | Newly collected instance ID, symbol, position, and configured increment. Increment can exceed the actual increase when capped by the strategy maximum. |
| `RawPayoutUnits` | Ordinary payouts plus eligible scatter payout before the round cap. |
| `PayablePayoutUnits` | This grid's amount after applying remaining round allowance. |
| `CapAdjustmentUnits` | `RawPayoutUnits - PayablePayoutUnits`. |
| `Transition` | Removals, movements, arrivals, next board, and next unused instance ID; null if no refill is executed. |

A position has `Reel` and `Row`, both zero-based. Movement evidence uses an instance ID
and `From`/`To` positions. Arrival evidence uses a new `Instance` and its `Position`.
The next step's grid is the prior transition's grid. A capped step has no transition.

An `AwardPayout` contains `Award`, `AppliedMultiplier`, and `RawPayoutUnits`.
Its `WinAward` contains `PaylineId`, `Symbol`, `MatchCount`, `PaytableMultiplier`,
`BasePayoutUnits`, contributing `Positions`, and the subset `WildPositions` used as
substitutes. `PaylineId` is nullable for custom evaluators; built-in paylines provide it.

`ScatterResult` contains `Count`, `RequestedFreeSpins`, `BasePayoutUnits`, `Positions`,
and `CountedInstanceIds`. When counting at most one per reel, more IDs can be inspected
than positions count, so the ID array and position array need not have equal length.

## Why raw amounts cannot always be summed for payment

Change the tutorial's round cap multiplier to 6. The paid spin pays 500 of a 600-unit
round allowance. The first free spin calculates a 300-unit raw award but has only
100 allowance left:

```text
RawPayoutUnits       300
PayablePayoutUnits   100
CapAdjustmentUnits  200
TotalPayoutUnits    100    (this free spin)
RoundPayoutUnits    600    (paid spin + this free spin)
CompletionReason   WinLimit
NextBonus          null
```

Individual ordinary award records remain raw; they are evidence of the calculation.
Use completed payable totals for the host's chosen settlement scheme. Scatter requests
may still be present in evidence even when no feature is actually granted due to a cap.

## `BonusState`: starting the next free spin

| Fields | Meaning |
| --- | --- |
| `OriginatingRoundId` | Evaluation ID of the paid spin that began the bonus. |
| `GameFingerprint`, `EngineVersion`, `RulesVersion` | Identity checked before using the state. |
| `CalculationStakeUnits` | Exact triggering stake, which may not change during the bonus. |
| `RemainingSpins` | Number of free spins still available. |
| `TotalSpinsAwarded` | Lifetime grant count, including retriggers. |
| `CompletedSpins` | Finished free spins. Remaining equals awarded minus completed. |
| `Multiplier` | State for the next free spin; a Spin-persistent strategy stores its start value here. |
| `RoundPayoutUnits` | Paid-round payable amount accumulated before the next spin. |

Store the engine-returned state rather than calculating or editing these counters.
The final free spin can retrigger and return another bonus; do not assume it ends
merely because the input had one remaining spin.

## `SpinContinuation`: finishing the current spin

| Fields | Meaning |
| --- | --- |
| `GameFingerprint`, `EngineVersion`, `RulesVersion` | Required matching identities. |
| `Request` | Original evaluation ID, calculation stake, and starting bonus (explicitly null for paid). |
| `Grid`, `InitialStops` | Next board to evaluate and the original initial stops. |
| `NextInstanceId`, `NextGridIndex`, `NextDrawOrdinal` | Next unused symbol ID, next grid number, and next external draw position. |
| `Multiplier` | Current strategy state. |
| `CollectedInstanceIds`, `CountedScatterInstanceIds` | Deduplication history for this spin. |
| `SpinPayoutUnits`, `RoundPayoutUnits` | Cumulative payable amounts. |
| `PendingFreeSpins` | Feature requests waiting for this spin to finish. |

Use `SpinStateSerializer.Serialize`, `DeserializeContinuation`, and `DeserializeBonus`
as demonstrated in [integration](integration.md). Preserve every field. Default values
cannot be dropped: missing `PendingFreeSpins`, for example, could lose earned spins.
Strict loading rejects missing, duplicate, unknown, and invalid-null properties.
Engine validation then checks game-dependent invariants. This does not authenticate
state supplied by a player; the host must trust and protect its stored state.

## Simulator statistics

RTP is `(base payout + bonus payout) / paid stake`. The denominator excludes free-spin
calculation stakes because those spins charge zero. Base/bonus contributions use the
same paid-stake denominator and sum to RTP. Shares of total payout divide each payout
by `base payout + bonus payout` and sum to 100% when any payout was observed; both
shares are zero otherwise. See [current targets and measurements](simulation-and-math.md#targets-and-measured-calibration)
for a worked payout split. Paid-spin scatter cash counts toward the
base payout. Round hit frequency counts any positive
round payout; bonus frequency counts paid rounds that actually start a bonus.

Retriggers count completed free spins that actually grant more spins, not every
scatter appearance. Refill counts count cascade transitions, not individual replacement
cells. Payout bands classify complete-round gross payouts in stake multiples.
Applied-multiplier distribution counts evaluated grids. The reported largest observed
multiplier considers both applied and post-grid state values.

For multiple sampled rounds, standard deviation and the approximate 95% RTP interval
use complete rounds, not correlated individual free spins. The largest observed win
is a sample maximum, not proof of the largest reachable win. `tutorial` and scripted
demo payout ratios are not RTP estimates.

Simulation limits apply to the whole paid round, including its free spins. If a grid
or spin budget is exceeded, or Ctrl+C cancels execution, the command fails without
publishing a successful report for a truncated run. These operational limits do not
change game payouts or make an unfinished bonus into a completed round. See the
[command reference](start-here.md) for the limit options.

For the smallest complete example using these same results, see the
[plain-payline integration](integration.md#a-complete-plain-payline-example).
