# JSON game configuration reference

This page covers every field accepted by `GamePackageLoader.Load` for schema 1 and
profile `slot-engine-v1`. Start with [PaylineGame.json](../samples/PaylineGame.json) for
the simplest included package, or [FirstGame.json](../samples/FirstGame.json) for
[the editing tutorial](create-a-game.md). The loader builds a
`GameDefinition` used by `SlotEngine`, with or without cascades.

Property names use the exact camelCase spelling shown below. JSON accepts no comments,
trailing commas, duplicate object keys, or unknown fields. Enum settings are strings,
not integers. The loader limits input to 2,000,000 characters and nesting depth 32.
Missing required fields are errors. Optional nullable fields may be omitted or null;
the defaults below describe both. Optional booleans must be actual booleans when present.

## Top-level fields

| Field | Required? / default | Meaning and restrictions |
| --- | --- | --- |
| `schemaVersion` | Required | Integer `1`. Describes the package format. |
| `profile` | Required | String `"slot-engine-v1"`. Selects the supported rule family. |
| `gameId` | Required | Nonblank game identifier, for example `"first-game-tutorial"`. |
| `mathVersion` | Required | Nonblank label identifying a mathematics release, for example `"tutorial-1"`. Update it when changing game rules for a release. |
| `visibleRows` | Required | Integer 1–128. Number of visible cells on each reel. |
| `symbols` | Required | Array of 1–4,096 unique non-negative integer IDs. IDs may include 0. Every symbol reference must be declared here. |
| `reels` | Required | Array of 1–128 nonempty arrays of symbol IDs, one circular strip per reel. Order matters. |
| `freeSpinReels` | Optional; uses `reels` | Separate strips for free spins. Same reel count, nonempty strips, declared symbols. Strip lengths may differ from paid strips. |
| `paylines` | Required | Nonempty array, at most 100,000 line objects. All are active. IDs and complete paths must be unique. |
| `paytable` | Required | Nonempty array of ordinary award entries. Described below. |
| `allowedStakes` | Required | Nonempty array of unique positive integer amounts. Each must divide evenly by the number of paylines. These are total calculation stakes in host-defined units. |
| `cascadePolicy` | Required | `"none-v1"` evaluates one board; `"falling-symbols-v1"` removes selected winning positions, applies gravity, and refills. |
| `paidRefillWeights` | Required for `falling-symbols-v1`; omitted/null for `none-v1` | One nonempty weighted-symbol table per reel. Used for replacement symbols, not initial stops. |
| `freeRefillWeights` | Optional for `falling-symbols-v1`; uses `paidRefillWeights` | One weighted table per reel for free-spin replacements. Omit/null for `none-v1`. |
| `wilds` | Optional; no substitution | Wild-symbol and substitution-target lists. Wilds can also have own-symbol paytable awards. |
| `symbolPriority` | Optional; empty | Unique declared symbol IDs in preferred order for award ties. Unlisted symbols rank after listed ones; numeric ID breaks remaining ties. |
| `scatters` | Optional; no scatter evaluation | One scatter symbol with paid and optional free threshold tables. |
| `maximumAwardedFreeSpins` | Optional; free-spin feature disabled | Positive integer lifetime allowance across the entire bonus, including its initial grant and all retriggers. Must be present if any scatter threshold requests free spins. |
| `paidMultiplier` | Optional; constant 1x | Multiplier configuration used during paid spins. Its persistence must be `Spin`. |
| `bonusMultiplier` | Optional; constant 1x | Multiplier configuration used during free spins. |
| `roundWinLimitMultiplier` | Optional; no cap | A positive integer multiple of calculation stake caps gross payout across the paid spin and all associated free spins. Omission or null selects `NoWinLimit`; zero is invalid. |
| `multiplyScatterAwards` | Optional; `false` | Whether monetary scatter awards receive the current feature multiplier. Does not multiply the number of free spins. |
| `allowScatterRefills` | Optional; `false` | Allows the configured scatter in refill tables. Requires `falling-symbols-v1` when true. Evaluation timing still determines whether refilled scatters can award anything. |

Symbols and counts use 32-bit signed integers; stakes, monetary multipliers, and
collection increments use 64-bit signed integers. Configuration validation also rejects
combinations whose conservative payout bounds exceed 64-bit storage, even if individual
fields fit. A low round cap does not bypass raw-award overflow validation.

## A complete package without cascades

This package selects one board per spin and has no payout cap. With initial stops
`[0,0,0]`, its only line pays `100 × 5 = 500` units:

```json
{
  "schemaVersion": 1,
  "profile": "slot-engine-v1",
  "gameId": "plain-example",
  "mathVersion": "1",
  "visibleRows": 1,
  "symbols": [1, 3],
  "reels": [[1, 3], [1, 3], [1, 3]],
  "paylines": [{"id": 0, "rows": [0, 0, 0]}],
  "paytable": [{"symbol": 1, "matchCount": 3, "multiplier": 5}],
  "allowedStakes": [100],
  "cascadePolicy": "none-v1"
}
```

To enable cascades, select `falling-symbols-v1` and supply `paidRefillWeights`.
Both policies can use optional wilds, scatters, free spins, and multipliers.
With `none-v1`, a win ends after its current board; it can still award a next free spin.
Refill tables and `allowScatterRefills: true` are rejected with `none-v1` because no
refill occurs. A round cap is independent of the cascade policy.

## Reel strips and paylines

`[8,1,3]` is one strip. Stop 0 with two visible rows displays 8 above 1. Stop 2 displays
3 above 8 because the strip wraps. A window longer than its strip repeats symbols;
that is permitted. Initial stops come from draw requests with the strip length as
exclusive upper bound. Uniform external draws make strip indices equally likely.

A payline object has exactly these required fields:

| Field | Example | Meaning |
| --- | --- | --- |
| `id` | `0` | Unique non-negative integer ID. |
| `rows` | `[1,1,1]` | One zero-based row for each reel; every row must be less than `visibleRows`. |

Two lines may overlap cells but cannot be identical paths. With four lines and a
100-unit stake, each line receives a 25-unit stake. A 101-unit stake would be invalid.

## Ordinary paytable entries

All three fields are required:

| Field | Example | Meaning |
| --- | --- | --- |
| `symbol` | `1` | Declared target symbol ID. Cannot be the configured scatter or a collection symbol. |
| `matchCount` | `3` | Positive consecutive match length, no greater than reel count. |
| `multiplier` | `5` | Positive multiple of line stake, before applying a feature multiplier. |

An entry such as `{"symbol":1,"matchCount":3,"multiplier":5}` pays `lineStake × 5`.
There is no hardcoded three-symbol minimum: the configured positive lengths decide.
Matching starts at the leftmost reel and stops at the first nonqualifying symbol.
The longest defined qualifying length pays; missing lengths fall back to shorter
entries. Entries for the same symbol cannot decrease in multiplier as length increases.
Duplicate symbol/length pairs are invalid.

With wilds, several target symbols may qualify on one line. The engine selects one
by base payout, then longer awarded length, then `symbolPriority`, then lower symbol ID.
Other lines pay independently. Only positions in selected awards are removed.

## Refill weight entries

Each inner table contains objects with two required fields:

| Field | Example | Meaning |
| --- | --- | --- |
| `symbol` | `3` | Declared symbol ID, unique within this table. |
| `weight` | `2` | Positive integer relative weight. Table total must not exceed 2,147,483,647. |

For `[{"symbol":1,"weight":2},{"symbol":3,"weight":1}]`, draw 0 or 1 selects
symbol 1, and draw 2 selects symbol 3. With uniform draws the probabilities are 2/3 and
1/3. Entry order determines the mapping and participates in configuration identity.
Refills use independent weighted choices, not continuation along the original strip.

## Wild configuration

If `wilds` is present, both arrays are required (they may be empty):

| Field | Example | Meaning |
| --- | --- | --- |
| `symbols` | `[7]` | Declared IDs allowed to substitute. |
| `substitutesFor` | `[1,2]` | Declared ordinary target IDs. Must not overlap `symbols`. |

Scatters and collection symbols cannot be wilds or substitution targets. Wilds do not
substitute for each other. An all-wild line can qualify for ordinary targets and a
wild's own award if its paytable entry exists. A symbol does not become wild merely
because it uses ID 7; that is just a sample convention.

## Scatter configuration

| Field | Required? / default | Meaning |
| --- | --- | --- |
| `symbol` | Required | One declared, non-paying symbol ID, distinct from collection and wild roles. |
| `paidThresholds` | Required | Array of threshold objects; an empty array disables paid awards. |
| `freeThresholds` | Optional; empty | Free-spin awards/retriggers. It does **not** inherit the paid table. |
| `atMostOnePerReel` | Optional; `false` | Count all eligible scatter cells, or at most one counted position per reel. |
| `timing` | Optional; `"InitialGrid"` | `"InitialGrid"` or `"NewArrivalsEachGrid"`. |
| `allowMultipleFeatureAwards` | Optional; `false` | Permit more than one free-spin trigger batch within the same spin. |

Each threshold has:

| Field | Required? / default | Meaning |
| --- | --- | --- |
| `minimumCount` | Required | Positive minimum scatter count. Unique in its table. |
| `freeSpins` | Required | Non-negative requested number of spins; zero permits money-only awards. |
| `totalStakeMultiplier` | Optional; `0` | Non-negative monetary award factor based on **total** calculation stake. |

Only the highest satisfied threshold pays. Higher thresholds cannot decrease either
award. Thresholds must fit the board's maximum possible count (or reel count when
limited per reel). This dimension check does not prove that particular strips can
actually generate the threshold.

`InitialGrid` evaluates once per spin, including losing initial grids.
`NewArrivalsEachGrid` evaluates a batch of previously uncounted instances on each grid;
it does not accumulate unrelated batches until a threshold is reached. Falling instances
are not counted twice. Monetary scatter awards can pay on successive eligible batches
even when the single-feature-trigger setting suppresses more free spins.

## Multiplier configuration

Every non-null multiplier object requires `strategy` and a positive `start`.
Allowed strategy values and all remaining fields are listed here:

| Field | `constant-v1` | `paying-cascade-v1` | `collected-symbol-v1` |
| --- | --- | --- | --- |
| `start` | Required; fixed factor | Required; initial factor | Required; initial factor |
| `increment` | Not used | Required positive integer | Not used |
| `maximum` | Not used; equals start | Required; at least start | Required; at least start |
| `persistence` | Not used; always Spin | Required: `"Spin"` or `"Bonus"` | Required: `"Spin"` or `"Bonus"` |
| `includeInitialGrid` | Not used | Optional; `true` | Not used |
| `collectionValues` | Not used | Not used | Required nonempty array |
| `applyBeforeAward` | Not used | Not used | Optional; `true` |
| `requiresWin` | Not used | Not used | Optional; `false` |

Omit fields marked "Not used" (a non-null value is rejected). For strategy-specific
nullable options, null behaves like omission. Each `collectionValues` object requires
`symbol` (a declared non-paying ID, unique in the array) and `value` (positive increment).
Null entries are invalid.

`constant-v1` always applies `start`. `paying-cascade-v1` applies the current factor,
then increases it once when ordinary wins exist, limited by `maximum`.
`includeInitialGrid: false` suppresses that increase on grid 0 only. Scatter-only
awards do not cause an increase.

`collected-symbol-v1` adds values from newly collected instances, limited by `maximum`.
`applyBeforeAward` decides whether the new or old factor pays the current grid.
`requiresWin: true` collects only when ordinary wins exist. Collection symbols are
not removed simply for being collected; the same falling instance is not collected twice.

`Spin` resets the multiplier for each free spin. `Bonus` carries it across free spins
until the bonus ends. Paid multipliers must use Spin persistence. The paid spin's
multiplier never carries into the newly activated bonus; the bonus uses its own start.

Examples (individual objects, not complete packages):

```json
{"strategy":"constant-v1","start":2}
```

```json
{"strategy":"paying-cascade-v1","start":1,"increment":1,"maximum":3,"persistence":"Bonus"}
```

```json
{"strategy":"collected-symbol-v1","start":1,"maximum":50,"persistence":"Bonus","collectionValues":[{"symbol":9,"value":2}],"applyBeforeAward":true,"requiresWin":false}
```

## What configuration cannot do

This profile always uses paylines and circular initial strips. Cascades are either
disabled or use weighted independent refills and downward gravity. It cannot select ways, clusters, sticky/expanding wilds,
fractional multipliers, arbitrary scripts, or a different cascade algorithm by adding
JSON fields. Custom policy composition is available through the C# API.
See [the developer guide](implementation-guide.md).

Loading validates configuration consistency and arithmetic capacity. It does not
prove RTP, reachable maximum wins, desirable game behavior, or production readiness.
Use the [troubleshooting table](troubleshooting.md) for rejected packages.
