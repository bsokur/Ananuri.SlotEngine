# Terms used by this engine

Use this page alongside [Start here](start-here.md). Indices begin at zero: the first
reel is reel 0, the top row is row 0, and the first grid in a spin is grid 0.

| Term | Meaning here |
| --- | --- |
| Symbol / symbol ID | A game item represented by an integer, such as `1` for A. IDs carry no built-in artwork or behavior. Configuration assigns paying, wild, scatter, or collection roles. |
| Reel strip | An ordered circular list of symbols. It is longer than the visible window in many games. |
| Stop | An index into a reel strip. The symbol at that index appears at the top of the visible reel; following strip symbols appear below it, wrapping at the end. |
| Board / grid | The visible rectangle of symbols, addressed as `[reel, row]`. Every board tracks symbol-instance IDs, including games without cascades. |
| Reel-major | Flat storage lists every row of reel 0, then every row of reel 1, and so on. This differs from reading a printed board row by row. |
| Payline | One selected row on each reel. `[1,1,1]` is the bottom horizontal line on a three-reel, two-row board. |
| Paytable | Entries specifying a target symbol, consecutive match length, and payout multiplier. |
| Stake / bet | The amount used to calculate a spin. All configured paylines are active and share the total stake equally. |
| Unit | An integer amount chosen by the host. `100` might represent 1.00 in a currency if the host defines 100 units per currency unit. The engine has no currency or rounding conversion. |
| Line stake | Total calculation stake divided by active payline count. A 100-unit stake on four lines gives 25 units per line. |
| Paytable multiplier | Multiplies line stake to obtain an ordinary award's base payout. |
| Feature multiplier | An additional factor, such as a free-spin multiplier, applied to base awards. Distinct from the paytable multiplier. |
| Wild | A symbol configured to substitute for specified ordinary symbols. It can also have an own-symbol paytable entry. |
| Scatter | A designated symbol counted in eligible grid positions, without needing a payline. It can request free spins and/or a monetary award. |
| Collection symbol | A non-paying symbol whose newly seen instances can increase a configured multiplier. |
| Instance ID | Identifies one occurrence of a symbol within a spin. Two A symbols have different IDs. A falling symbol keeps its ID; a replacement gets a new one. IDs restart for another spin. |
| Cascade / refill | Remove selected winning cells, let survivors fall, and fill the empty cells. Then evaluate the resulting board. |
| Refill weight | Relative chance for a replacement symbol. Weights 2 and 1 correspond to probabilities 2/3 and 1/3 when the supplied integer draw is uniform. They need not total 100. |
| Spin / evaluation | One paid or free spin, including all its grids/cascades. A work budget may split its computation across calls. |
| Paid round | One paid spin plus all free spins it awards, including retriggers. Used for the round payout cap and simulator statistics. |
| Free spin / bonus | A spin with zero charged stake but the calculation stake retained from its originating paid spin. `BonusState` carries the remaining free spins. |
| Retrigger | Award extra free spins during a free spin, subject to the lifetime award allowance. |
| Raw payout | Calculated amount before the round cap. |
| Payable payout | Amount after applying the remaining round allowance. This is the amount counted in the result total. |
| Gross payout | Amount awarded without subtracting the paid stake. Net change would be gross payout minus stake, calculated by the host. |
| Win cap | Configured maximum gross payout across a paid round. Reaching it ends the round, including pending free spins. |
| Deterministic | Identical definition, requests/state, draw values, and engine behavior produce identical results. The engine does not decide random values itself. |
| Draw source / RNG | A provider of bounded integer choices. A real random number generator may back the host's draw source. Tutorial input is scripted, not random. |
| Seed | Starting input for the simulator's pseudorandom sequence. A seed alone is not a portable historical replay record across runtime changes. |
| Replay | Re-evaluate using the recorded draw requests and values, together with the matching game and engine artifacts. |
| Work budget | Maximum number of grids processed in one engine call. It pauses calculation; it does not change game rules or settle a spin. |
| Continuation / checkpoint | State needed to resume the same unfinished spin at its next grid. Different from bonus state, which starts the next free spin. |
| Game package | JSON data selecting the supported game rules and their parameters. It is not executable code. |
| Fingerprint | Content hash identifying a compiled game configuration. Changing configuration can change the fingerprint even if the display name stays the same. |
| Math version | Host-chosen label for a version of game mathematics. It is distinct from the engine package version and rule profile. |
| Host / runtime | The application calling the library. It owns players, trusted random allocation, durable state, and payment. |
| Idempotency | A host operation can be retried without allocating another outcome or charging/paying twice. Deterministic calculation alone does not implement this. |
| RTP | Return to player: total gross payouts divided by paid stakes over a distribution or sample. 87.5% theoretical RTP does not promise a particular player's next result. |
| Hit frequency | Fraction of observations with any positive payout, including payouts smaller than the stake. The cascading simulator reports this per paid round. |
| Volatility / standard deviation | How widely results vary around their mean. The simulator reports standard deviation of complete-round payouts in stake multiples. |
| Confidence interval | An approximate statistical range for sampled RTP under assumptions; not a payout limit or proof of exact theoretical RTP. |
| NuGet package | A distributable compiled .NET library (`.nupkg`). Different from a JSON game package. |

Continue with [the worked round](first-round.md).
