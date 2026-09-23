# Plain-payline configuration

For setup and basic concepts, begin with [Start here](start-here.md). A complete
plain-payline C# program is included in [integration](integration.md).

Rule contract identifier: `slot-engine-v1`.

This page describes the simplest configuration of the one `SlotEngine`: ordinary
paylines, `NoCascades`, `NoWinLimit`, no wild/scatter/free-spin features, and constant
1x multipliers. These are the defaults of a directly constructed `GameDefinition`.
The JSON equivalent uses schema 1, profile `slot-engine-v1`, and
`cascadePolicy: "none-v1"`, with refill and optional feature/cap settings omitted.
[PaylineGame.json](../samples/PaylineGame.json) is a complete example.

Use `SpinRequest` and `IRandomDrawSource` for every game. If initial stops are already
known, `FixedReelStops` supplies them through that same draw interface. There is no
separate plain-game engine or result model.

## Definition and grid

- Each game has a non-empty game ID and math version. The surrounding package registry must prevent an existing ID/version from being overwritten with different content.
- Symbol IDs are non-negative integers scoped to a definition. Every strip/paytable reference must point to a declared symbol. Zero is a valid declared ID.
- The game has a positive reel count and a positive, common visible row count. Reel lengths can differ.
- Strips are circular, ordered sequences. A stop is the index of the **top** visible symbol; rows extend toward increasing strip indices and wrap.
- Windows longer than their strip repeat its symbols. This is intentional.
- The engine accepts valid stops. It does not enforce a probability distribution. The tiny fixture analysis assumes independent, uniform stop selection. A weighted RNG mapping requires separate mathematical analysis.
- The configuration is trusted host input. The strict JSON loader applies its documented size/count limits; host upload authorization and further resource quotas belong to the host.

## Wins

- All configured paylines are active on every spin.
- Each line chooses exactly one row on every reel.
- Line IDs and paths are unique. Lines may overlap and share winning positions.
- Matching starts at reel zero and proceeds consecutively to the right, using ordinary symbol equality.
- Minimum qualifying count is determined by the paytable. There is no hardcoded three-match minimum.
- One award per line: choose the entry with the longest qualifying match count. If no entry exists at that exact count, consider shorter counts. Only positions corresponding to the awarded count are included in `WinAward.Positions`.
- Multipliers for a symbol must not decrease as match count increases. Duplicate symbol/count entries are rejected.
- Separate lines pay independently and their payouts are added, including when winning positions overlap.
- In this plain configuration there are no wilds, scatters, cascades, feature awards, or win caps. A symbol gains a special role only when its corresponding component is configured. Adding features still uses the same engine; see [the complete rules](engine-rules.md).

## Amounts

- All stakes and payouts use signed 64-bit integers in one host-defined unit scale.
- For example, a host can choose one unit = 0.01 GEL for a suitable game/integration. The engine does not infer currencies or decimal places.
- Total stake must be positive and present in the definition's allowed stakes.
- Every allowed total stake must divide evenly by the active line count. Line stake = total stake / line count.
- Paytable multipliers are positive integer multiples of line stake. Fractional multipliers are not supported.
- Payout = line stake multiplied by award multiplier. Gross payout is returned; the stake is not subtracted and not added back separately.
- Calculations are checked for overflow. Game loading also rejects configurations whose conservative bound (maximum allowed total stake times largest multiplier) exceeds `long.MaxValue`.
- That bound is deliberately conservative. It is not proof of an achievable maximum win, and it is not a payout cap.
- An optional `TotalStakeWinLimit` or JSON `roundWinLimitMultiplier` specifies a cap as part of the game's mathematics. Leaving it unset selects `NoWinLimit`, whose maximum is null. It does not substitute a very large implicit cap.

## Determinism, identity, errors

- The engine contains no RNG calls, player state, clock-dependent outcome logic, network operations, or database operations.
- Given identical definition contents, input and engine/rule versions, results are repeatable.
- Definitions, inputs and outputs own immutable collections. One engine instance can evaluate independent calls without sharing spin state.
- A `SpinEvaluation` carries identity/version labels, calculation/charged stake, initial stops, step evidence, total payout, and completion state. Its single `CascadeStep` contains the `SymbolBoard` and `AwardPayout` records even though no cascading occurs. The step type describes a grid evaluation; its name does not enable cascades.
- A completed plain spin has one step, no transition, no continuation, and no next bonus. Raw payout equals payable payout. `NoMoreWins` means no further board is selected and can therefore complete a winning board.
- Complete replay also needs the exact historical game package and engine artifact. A version label alone is insufficient if artifacts are overwritten. Retain content hashes at the package/runtime boundary.
- Invalid definitions and input combinations throw argument exceptions. These are host contract failures. HTTP status mapping and player-friendly errors belong to the runtime.
- Outcome calculation does not imply payment. Wallet settlement is a separate durable workflow in the runtime.
