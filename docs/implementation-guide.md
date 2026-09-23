# Developer guide for the current solution

For your first use, follow [Start here](start-here.md). This page is for someone ready
to change the code after understanding the [worked round](first-round.md).

## Choose the appropriate kind of change

| Goal | Start here |
| --- | --- |
| Change symbols, strips, weights, stakes, awards, or feature settings | [Create a game](create-a-game.md) and [configuration reference](configuration-reference.md) |
| Understand or change evaluation order | [SlotEngine.cs](../src/Ananuri.SlotEngine/SlotEngine.cs) and [GridStepEvaluator.cs](../src/Ananuri.SlotEngine/Execution/GridStepEvaluator.cs) |
| Add a new multiplier, scatter, or cascade policy | The corresponding feature folder under the library root, plus its tests |
| Expose a reviewed policy through JSON | Package DTOs, GamePackageCompiler, compatibility validation, and configuration documentation |
| Change command output or offline statistics | Simulator Commands, Reporting, and Statistics folders |
| Add a regression case | The corresponding mechanic folder in the test project |

Keep the calculator separate from the host. HTTP, wallets, databases, and production
random-allocation services belong in the calling application. One class library is
appropriate for the current calculation boundary; adding projects is not itself an
improvement.

## Specify behavior before changing it

Write down when the new rule runs, which symbols and modes it affects, how it interacts
with wilds/scatters/caps, what state it needs, and which random draws it consumes.
Calculate at least one expected result independently. The current rule references
are [plain-payline configuration](plain-payline-rules.md) and [engine rules](engine-rules.md).

Policies implement IGameComponent: they must be immutable and deterministic, validate
their configuration, and include all behavior parameters plus an implementation
version in ConfigurationKey. The fingerprint combines these keys with game data.
Never rely on hidden mutable state, clocks, or unrecorded random draws.

Each policy declares the symbol roles and capabilities needed to validate its
composition. The validator uses these interface contracts for built-in and custom
implementations alike:

| Interface | Required declarations |
| --- | --- |
| `IWinEvaluator` | `GetPayingSymbols(game)`, `SubstitutionSymbols`, and `MaximumBasePayout(game, calculationStake)` |
| `ISymbolSubstitutionPolicy` | `InvolvedSymbols`: every wild or substitution target |
| `IScatterEvaluator` | `Symbols`, `CanAwardFreeSpins`, and `MaximumAwardMultiplier` |
| `IMultiplierStrategy` | `CollectionSymbols`, multiplier bounds, and persistence |
| `ICascadePolicy` / `IRefillPolicy` | `RefillSymbols` / `Symbols`: every possible replacement symbol |
| `IFeaturePolicy` | `SupportsFreeSpins` |

Return immutable symbol sets, including empty sets for unused roles. These declarations
must describe actual behavior: undeclared symbols and incompatible roles are rejected.
Include symbols from both paid and free-spin modes. Execution checks ordinary award
symbols, reported substitutions, counted scatters, collections, and refill arrivals
against the selected policies' declarations. The payline evaluator also checks both
sides of every successful substitution, even when that candidate does not win.
For example, a custom scatter that can grant spins requires a feature whose
`SupportsFreeSpins` is true, and its symbols cannot overlap paying, collection, or
substitution roles. Scatter replacements require explicit refill opt-in.

An evaluator's `MaximumBasePayout` returns a non-negative `BigInteger` bound on the
sum of its ordinary awards for one grid in either spin mode, before feature multipliers.
Supply a conservative bound for every allowed stake. Construction combines it with
multiplier and scatter bounds and rejects totals that exceed `long` storage; a round cap does
not relax that requirement. Execution rejects evaluator output that exceeds its
declared bound. Do not reuse the payline formula for an evaluator with different rules.

Direct `GameDefinition` construction permits empty paylines and paytables when a
custom evaluator does not need them. `PaylineWinEvaluator` validates their presence,
requires even stake division, and supplies the paytable-derived payout bound. The
JSON profile still selects that evaluator and requires both collections.

Custom cascade outputs must preserve survivor identity, remove the requested
instances, allocate contiguous fresh IDs, and report exact movement/arrival evidence.
`Apply` accepts removals selected by that policy; an empty selection returns the
unchanged board without draws. `NoCascades` always selects nothing and rejects
nonempty removals. Custom multiplier outputs must respect their declared bounds.
Feature state must remain compatible with the lifecycle and cumulative payout semantics.
An award-selection policy may return only a supplied candidate or null; it cannot
invent or alter an award. Outgoing feature grants and bonus state are validated
before a completed result is returned, including game identity, stake, counters,
cumulative payout, and payout-cap termination.

These interfaces retain the engine's board, spin, and free-spin lifecycle. Ways,
clusters, player decisions, and sticky/expanding symbols still need explicit rules
and contract tests before implementation; not every future mechanic fits unchanged.

## Protect state and replay compatibility

Keep definitions immutable after compilation. Persist returned continuation and bonus
state using SpinStateSerializer. Existing complete serialized field names remain part
of the integration contract; missing state fields are intentionally rejected.

A change to award selection, timing, draw order/bounds, state shape, or component
behavior can affect historical replay. Assign the appropriate engine/rule/component
versions for releases, and retain exact prior artifacts. A label is not enough if a
package artifact is overwritten. Do not migrate active bonuses by replacing their
fingerprints or resetting fields.

JSON support requires an explicit compiler change. A package cannot load arbitrary
types or scripts. Document new fields and test omitted/default, invalid, and compatible
combinations, including errors before random allocation where applicable.

## Verify the change

Use xUnit to test the public result or component contract, not merely a copy of the
implementation. Relevant checks include independently expected payouts, missing award
lengths, overlapping wins, raw/payable cap behavior, feature timing, state rejection,
and full versus resumed replay with multiple work budgets.

The documented tutorial is compiled and tested; its actual payouts must continue to
match the guides. The fixed-payline reference vectors are embedded directly into the
tests. Keep expected mathematics independent of the implementation under test.

Run the verification script from PowerShell:

```powershell
./scripts/verify.ps1
```

It restores, builds Release, checks formatting, runs tests, and asserts the tutorial,
enumeration and seeded simulation/replay results. It checks local Markdown links,
creates a local package in artifacts, verifies its contents and XML API documentation,
and builds and runs both complete integration examples against that package in a
temporary consumer with an isolated NuGet cache. It also executes the complete JSON
example from the configuration reference. These checks run automatically on every
verification; no manually copied snippet or existing package cache is relied on.
Both host fragments in engine-rules.md are compiled with supplied parameter stubs.
The game-creation exercise runs in a temporary directory and verifies its edited
1,500-unit payout. The current-game settings table is compared with JSON, and the
documented calibration fingerprint must match the compiled simulator result.
The script does not publish the package, configure remote CI, or establish exact
theoretical RTP for the larger samples.
See [verification status](verification.md).

Profile before optimizing. Preserve stable draw order and complete evidence when
reducing allocations. The one coordinator composes independent win evaluation,
continuation, feature, multiplier, and limit policies. Add a new mechanic at its
component boundary instead of duplicating the engine or creating parallel result types.

## API and package boundary

The API consists of `SlotEngine`, `GameDefinition`, `SpinRequest`, and
`SpinEvaluation`. Use `FixedReelStops` when a test or
host already has explicit initial stops.

JSON packages use schema 1, profile `slot-engine-v1`, and an explicit `cascadePolicy`.
Cascading packages select `falling-symbols-v1` and supply refill tables.
One-board packages select `none-v1` and omit refill configuration. A round cap is
optional. Public types use their feature namespaces; see the
[namespace map](architecture.md#public-namespaces) for the required imports.

## Keep the documentation usable

Update the field reference when DTOs/defaults change, the results guide when output
contracts change, and the worked example when teaching rules change. Execute commands
and sample code from a clean build, verify relative file paths, and update the
solution's Documentation entries so IDE users can find new pages.

Use [architecture](architecture.md) as the code map and [troubleshooting](troubleshooting.md)
for user-facing error explanations. Keep these guides focused on the supported API.
