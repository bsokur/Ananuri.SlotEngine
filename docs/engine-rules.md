# Slot engine rules and component contracts

This is the detailed rule reference. First-time readers should use [Start here](start-here.md),
[the worked round](first-round.md), and [the configuration field reference](configuration-reference.md).

Version 0.1.0 exposes one coordinator: `ISlotEngine` / `SlotEngine` in
`Ananuri.SlotEngine`. It accepts `GameDefinition` from `Ananuri.SlotEngine.Definitions`
and returns `SpinEvaluation` for every supported game. The rule identity and JSON
profile are `slot-engine-v1`; the JSON package schema version is 1. Payline evaluation
and cascading are independent components of the definition.

This is a calculation library, not a wallet or production random service. The larger
sample games are experimental fixtures. CascadingGame is experimentally calibrated;
the other fixtures are untuned, and none is independently certified. See
[simulation and game math](simulation-and-math.md) for targets and measured evidence.
Numerical limits in the samples are explicit game
rules, not defaults silently imposed by the coordinator.

## Quick start

```shell
dotnet run --project tools/Ananuri.SlotEngine.Simulator -- game-demo
dotnet run --project tools/Ananuri.SlotEngine.Simulator -- game-simulate 10000 12345
dotnet run --project tools/Ananuri.SlotEngine.Simulator -- game-demo samples/CollectedSymbolsGame.json
dotnet run --project tools/Ananuri.SlotEngine.Simulator -- game-simulate 10000 12345 samples/CollectedSymbolsGame.json
```

The simulator's `game-demo` command forces only the first paid spin's initial stops to zero, then uses an offline
seeded source. It completes the associated bonus and verifies an exact replay of every
evaluation using the recorded requests and values. Its scripted payout/stake ratio is
not an RTP estimate. The sampling mode uses uniform independent initial stops and the
configured refill weights.

## Reusable definitions and components

Load a package once and cache the resulting `GameDefinition`:

The following is a host integration fragment: `runtimeDrawSource` is supplied by your
application. For a complete executable example with every dependency defined, follow
[integration](integration.md) or run the `tutorial` command.

```csharp
using Ananuri.SlotEngine;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

var game = GamePackageLoader.Load(File.ReadAllText("game.json"));
ISlotEngine engine = new SlotEngine();

// supplied by the host: durable, trusted random allocation/replay implementation
IRandomDrawSource draws = runtimeDrawSource;
var request = new SpinRequest("unique-evaluation-id", 100);
var result = engine.Evaluate(game, request, draws);
```

`GamePackageLoader.Load` accepts strict JSON. Unknown members, duplicate object keys,
unsupported profiles/strategies, missing required fields, and incompatible roles are
rejected. It does not load scripts or types named by a package. `GamePackage` is a
transport DTO; compilation captures its collections in immutable definitions.

`GameDefinition` can also be constructed directly with tested implementations
of the component interfaces. Custom implementations must be immutable, deterministic,
validate their parameters, and include every behavior parameter and implementation
version in `ConfigurationKey`. The loader deliberately supports the built-in profile
only; new JSON strategy IDs require a reviewed loader change.

| Component | Responsibility |
| --- | --- |
| `IGridGenerator` | Initial grid and initial stop evidence |
| `IWinEvaluator` | Ordinary awards and their contributing positions |
| `ISymbolSubstitutionPolicy` | Substitution eligibility |
| `IAwardSelectionPolicy` | Select one candidate award per payline |
| `ICascadePolicy` | Select removals and transform/refill the grid |
| `IRefillPolicy` | Replacement symbol selection |
| `IScatterEvaluator` | Scatter events and award requests for each eligible grid |
| `IMultiplierStrategy` | Current award multiplier and next multiplier state |
| `IFeaturePolicy` | Validate and transition free-spin state |
| `IWinLimitPolicy` | Optional gross payout limit for the complete paid round |

Policies declare their symbol roles and capabilities through these interfaces.
Configuration validation checks declared symbols, role conflicts, scatter refill
permission, and whether scatter grants have a compatible free-spin feature.
`IWinEvaluator` also declares a conservative `BigInteger` bound on total ordinary
base payout for a grid at each stake. These checks apply to custom implementations
as well as built-ins; see [the developer guide](implementation-guide.md) for the
required members.

The JSON profile uses the payline evaluator and reel-strip grid generator. Select
`cascadePolicy: "none-v1"` for one board, or `"falling-symbols-v1"` for weighted refills
and downward gravity. Direct construction defaults to `NoCascades`, `NoWinLimit`,
ordinary paylines, no scatter/free-spin features, and constant 1x multipliers.
`NoCascades` affects board continuation only; optional wilds, scatter awards, and free
spins still work. Each free spin then evaluates its own single board.
Ways, clusters, expanding/sticky wilds, and fractional multipliers are not implemented.
Their semantics must be specified separately rather than inferred from these interfaces.

## Grid and ordinary awards

The following award rules describe the built-in `PaylineWinEvaluator`. A custom
evaluator may use different rules and may omit paylines and the paytable when
constructed through C#; it must declare its paying symbols and payout bound.
The JSON profile always uses the built-in evaluator.

- The initial stop identifies the top visible symbol. Windows wrap around strips.
- Base and free-spin reel sets are separately configurable; omitted free reels reuse
  the base set. Dimensions and declared symbols must agree.
- Cells are reel-major and carry an immutable symbol-instance ID. Gravity preserves
  that ID. Refills allocate new ascending IDs, starting after the initial grid IDs.
- All configured paylines are active. Calculation stake must divide evenly among them.
- For every paying target symbol, the evaluator counts a left-to-right qualifying run.
  Exact symbol equality and permitted wild substitution both qualify.
- For each target, the longest defined qualifying entry is selected; missing lengths
  fall back to shorter entries, retaining the original paytable convention.
- Candidate awards are compared by base payout, then awarded length, then configured
  symbol priority, then numeric symbol ID. Only one candidate pays per line.
- Wilds may have own-symbol awards if the paytable contains them. Wilds do not
  substitute for one another, scatters, or collection symbols. An all-wild run can
  qualify for ordinary targets and a configured own-symbol award.
- Awards record both contributing positions and positions used as substitutes.
- Different lines pay independently. Shared winning cells are removed only once.
- The built-in cascade policy removes only positions in the selected awards. It
  preserves survivors' vertical order and moves them down within their reel.
- Refill draws occur by ascending reel, then ascending empty row. Tables are separate
  from initial strips; each draw is mapped through positive integer weights.
- Replacements are independent weighted draws, not strip continuation. Free-spin
  refill tables may differ; omitted free tables reuse paid tables.

## Scatter rules

`GridScatterPolicy` evaluates the highest satisfied threshold in its mode's table.
Thresholds contain minimum count, requested free spins, and an optional integer award
in multiples of total calculation stake. Higher thresholds may not decrease awards.

Counting can include every eligible cell or at most one per reel. Supported timing:

- `InitialGrid`: evaluate once per spin, including a losing initial grid.
- `NewArrivalsEachGrid`: evaluate each grid using only previously uncounted scatter
  instances. This is a per-grid batch of new arrivals, NOT accumulation of scattered
  arrivals across multiple grids. Existing symbols falling to new rows do not count
  again. All newly inspected scatter IDs are recorded, including extra scatters on a
  reel when the count is limited to one per reel.

By default only one feature trigger is allowed per spin. `allowMultipleFeatureAwards`
explicitly permits later eligible batches to add awards. Monetary scatter awards can
pay for each qualifying new batch. The ordinary-paying-cascade multiplier does not
increase due solely to scatter awards. `multiplyScatterAwards` separately determines
whether scatter monetary awards receive the current multiplier.

Scatter refill weights require `allowScatterRefills: true`. With initial-only timing,
refilled scatters do not trigger. Scatters do not appear in ordinary paytables and are
not removed by the built-in awarded-position cascade policy; they can fall.

For CascadingGame's paid scatter cash and free-spin tiers, see the
[current CascadingGame rules](simulation-and-math.md#current-rules).

## Free spins, stake, and lifecycle

`SpinRequest.Bonus == null` starts a paid evaluation. Otherwise it evaluates one free
spin using trusted runtime-supplied `BonusState`.

- Paid spins charge their calculation stake. Free spins charge zero and use the exact
  calculation stake captured from the triggering paid spin.
- One free spin includes all of its cascades. It consumes one spin only on completion.
- The paid spin finishes before activating its pending bonus. Its multiplier does not
  carry into the bonus: the bonus strategy initializes its own state.
- Retriggers add spins after the current free spin resolves. A final-spin retrigger
  extends the same bonus. It does not reset a bonus-persistent multiplier.
- `maximumAwardedFreeSpins` bounds the total number granted throughout the bonus,
  including its initial grant. A retrigger receives only the remaining allowance.
- A losing free spin still consumes one spin. A bonus-persistent multiplier remains.
- With no remaining spins after retriggers, `NextBonus` is null.
- Each bonus retains originating round ID, package fingerprint, engine/rules versions,
  triggering stake, counts, multiplier, and cumulative paid-round payout.
- `FreeSpinsFeature.ValidateState` rejects incompatible packages/versions, changed
  stakes, inconsistent counters, and invalid multiplier/payout state before draws.

## Multiplier strategies

`Apply` is a pure transition. `AppliedMultiplier` and `NextState` are intentionally
separate, preventing a post-award increase from affecting the current payout.

| Strategy ID | Semantics |
| --- | --- |
| `constant-v1` | Fixed positive factor |
| `paying-cascade-v1` | Apply current value, then increment once if ordinary wins exist |
| `collected-symbol-v1` | Add configured values from newly collected symbol instances |

Paying-cascade parameters: start, positive increment, maximum, persistence (`Spin` or
`Bonus`), and `includeInitialGrid`. Reaching maximum leaves the value at maximum.

Collection parameters: start, maximum, persistence, per-symbol positive increments,
`applyBeforeAward`, and `requiresWin`. Eligible symbols are collected once per instance,
in reel/row order. They remain non-paying cells, can fall, and do not cause refills on
their own. The reported collection value is its configured increment; the before/after
state reflects any multiplier maximum. IDs already collected survive checkpoints.

There is one multiplier strategy per mode. Paid strategies must use spin persistence.
Bonus strategies with spin persistence reset between free spins. Bonus persistence
retains the final value until the feature ends. Simultaneous strategy composition is
not defined in this profile.

CascadingGame's multiplier configuration is in the [current rules](simulation-and-math.md#current-rules).
The alternative CollectedSymbolsGame sample demonstrates collection before awards
and persistence through a bonus.

## Evaluation order and limits

1. Validate the request/state and establish the mode and calculation stake.
2. Generate the initial grid with externally supplied draws.
3. Evaluate the grid's eligible scatter batch and ordinary win candidates.
4. Run the multiplier strategy and record collections.
5. Calculate ordinary awards and any eligible scatter award using checked integers.
6. If a payout cap is configured, apply the remaining paid-round allowance.
7. If the cap is reached, complete the round immediately: no refill, pending bonus,
   or further multiplier advancement. Retain raw and adjusted award evidence.
8. Otherwise retain next multiplier state and select cascade removals.
9. If removals exist, apply gravity/refill and repeat from step 3.
10. Otherwise apply pending feature awards/retriggers and complete the spin.

`TotalStakeWinLimit` caps gross payout across the paid spin AND its associated bonus.
It does not subtract stake. A final grid can have raw awards above its remaining allowance; the result
records `RawPayoutUnits`, `PayablePayoutUnits`, and `CapAdjustmentUnits`. Individual
award amounts remain raw; do not sum them to settle a capped result.

`NoWinLimit.MaximumPayout` returns null, representing no mathematical round cap.
The JSON equivalent is an omitted or null `roundWinLimitMultiplier`. No cap is
silently substituted; checked 64-bit arithmetic still applies to all cumulative
amounts. A configured cap must be positive and fit the calculation stake.

For every allowed stake, game loading combines the selected evaluator's ordinary
payout bound with the maximum paid/bonus multiplier and scatter award bound. It
rejects totals that cannot fit in `long`, with or without a round cap. Execution
also rejects ordinary awards exceeding the evaluator's declared base payout bound.
Custom components must honor their declared bounds. Overflow or invalid component
output raises an error; it is never converted into a loss.

## Resumable work and historical replay

`maxGridEvaluations` is a technical work budget, not a game maximum. If it is reached,
the result has `CompletionReason.WorkBudget`, `IsComplete == false`, and a continuation
at the next grid boundary. No bonus spin is consumed until evaluation completes.

```csharp
var result = engine.Evaluate(game, request, draws, maxGridEvaluations: 100);
while (result.Continuation is not null)
{
    // In production, retain prior Steps and persist checkpoints/draw allocation.
    result = engine.Resume(game, result.Continuation, draws, maxGridEvaluations: 100);
}
// Persist the completed outcome and NextBonus consistently, then settle idempotently.
```

`Steps` contains only the steps produced by that call. `TotalPayoutUnits` is cumulative
for the spin, and `RoundPayoutUnits` includes its originating paid spin and earlier
bonus spins. Do not sum cumulative totals across resume calls. `ChargedStakeUnits`
describes the original wager on every response, NOT another debit instruction.

Continuations and bonus state round-trip through `System.Text.Json`. They are trusted
runtime records, not authenticated player tokens. Version/fingerprint validation
detects accidental mismatches, not malicious forgery. Use state-version checks in the
runtime to prevent two requests from consuming the same free spin.

Use `SpinStateSerializer.Serialize(state)`, `DeserializeContinuation(json)`, and
`DeserializeBonus(json)` for persistence. This serializer rejects missing constructor
fields, unknown or duplicate properties, and null non-nullable fields. Persist the
complete representation, including zeros and an explicit null `SpinRequest.Bonus` for
paid spins; omitting default values is not supported. State records also mark their
persisted properties as required when using the default `JsonSerializer`. Valid JSON
field names and numeric representations remain unchanged. Game-specific validation
still runs in `Evaluate` and `Resume` before random draws. Older incomplete records
must be recovered from authoritative persisted outcomes, not filled with guessed defaults.

Cascade transitions are checked against their input and output boards. Removed IDs
must disappear, all survivors must retain their IDs and symbols, and fresh IDs must
fill the contiguous interval starting at `NextInstanceId`. Every actual movement and
arrival must appear exactly once in the transition evidence. These checks do not
impose downward gravity on a custom policy.

`ICascadePolicy.Apply` accepts removals selected by that same policy. An empty
selection returns an identity transition without requesting draws. `NoCascades`
always selects no removals, supports that identity transition, and rejects nonempty
removals because it does not define a removal/refill operation.

`IRandomDrawSource` receives evaluation ID, draw ordinal, purpose, and exclusive upper
bound. Values must be within the range. Initial stops and refills use the same ordered
sequence. Retain exact package and engine artifacts as well as version labels.
Sources may support a finite set of predetermined requests and reject unavailable
or mismatched requests. Select a source that covers the evaluation's required draws:
`FixedReelStops` supplies initial stops only, while cascades can require refills.

`RecordingDrawSource` is in-memory memoization for scripted examples and tests. It returns a previously
recorded value for the same request and rejects a changed request at the same ordinal.
`ReplayDrawSource` reads captured allocations and rejects missing or mismatched draws.
Neither is a durable production allocation service. The runtime must durably associate
random allocation with an evaluation so a crash cannot allocate a new funded outcome.
The runtime also owns settlement idempotency, synchronization, and retry recovery.

Package fingerprints include configuration values, selected component keys, and rule
identity. Custom strategy authors must update their version keys when behavior changes.
Byte-identical historical behavior additionally requires the original engine artifact.

## Results and statistics

Each grid includes the evaluated board, selected ordinary awards and substitutions,
scatter result, collections, before/applied/after multipliers, raw/payable totals,
cap adjustment, and optional removal/movement/refill transition. The complete result
includes starting and next bonus state and explicit completion reason.

The simulator completes every originating paid round and its bonus before accumulating
round statistics. RTP denominator contains paid stakes only. It reports base/bonus
contributions, round hit frequency, bonus frequency, retriggers, payout bands, refill
counts, bonus lengths, applied multiplier counts, largest observed win, and cap hits.
The standard deviation and approximate normal 95% interval use complete independent
round observations, not correlated individual free spins. For rare-tail game math,
this approximate interval is not a substitute for dedicated mathematical analysis.

Elapsed time includes generation, engine execution, and statistics. Console output and
scripted replay verification are excluded. Seeded `System.Random` exists only in tooling;
it is not a production random source or a portable cross-runtime replay contract.

## Extension boundaries

Use the same `SlotEngine` and cached definitions for plain and featured games.
New policies can be injected programmatically; they must obey the
public immutable contracts and have independent contract tests. Supporting arbitrary
state machines, player decisions, ways/clusters, expanding/sticky wilds, or new refill
families requires specifying those rules and, where necessary, a new rule profile.

Do not mutate a definition to tune RTP during play or reinterpret an active bonus with
a replacement package. Mathematical release work still includes probability analysis,
reachable award checks, termination analysis, and the intended deployment's testing.
