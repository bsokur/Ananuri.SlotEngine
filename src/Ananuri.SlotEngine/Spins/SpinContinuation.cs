using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Spins;

/// <summary>A checkpoint at a grid boundary. Persist it with the associated draw allocation.</summary>
/// <param name="GameFingerprint">Canonical game fingerprint binding state and results to exact rules.</param>
/// <param name="EngineVersion">Engine implementation version required for matching persisted state.</param>
/// <param name="RulesVersion">Engine rules version required for matching persisted state.</param>
/// <param name="Request">Original spin request, retained unchanged across resumptions.</param>
/// <param name="Grid">Immutable board for this evaluation boundary.</param>
/// <param name="InitialStops">Zero-based initial reel stops, one per reel in left-to-right order.</param>
/// <param name="NextInstanceId">Next unused evaluation-local symbol-instance ID.</param>
/// <param name="NextGridIndex">Index of the next grid to evaluate.</param>
/// <param name="NextDrawOrdinal">Ordinal of the next random draw to allocate.</param>
/// <param name="Multiplier">Current positive multiplier carried into the next grid.</param>
/// <param name="CollectedInstanceIds">Instance IDs already collected in this spin, preventing collection again after movement.</param>
/// <param name="CountedScatterInstanceIds">Instance IDs already counted as scatters during this spin.</param>
/// <param name="SpinPayoutUnits">Cumulative payable payout of the partially evaluated spin.</param>
/// <param name="RoundPayoutUnits">Cumulative payable payout for the paid spin and all free spins in its round.</param>
/// <param name="PendingFreeSpins">Requested free spins accumulated so far, before feature limits at spin completion.</param>
public sealed record SpinContinuation(
    [property: JsonRequired] string GameFingerprint,
    [property: JsonRequired] string EngineVersion,
    [property: JsonRequired] string RulesVersion,
    [property: JsonRequired] SpinRequest Request,
    [property: JsonRequired] SymbolBoard Grid,
    [property: JsonRequired] ImmutableArray<int> InitialStops,
    [property: JsonRequired] long NextInstanceId,
    [property: JsonRequired] int NextGridIndex,
    [property: JsonRequired] long NextDrawOrdinal,
    [property: JsonRequired] long Multiplier,
    [property: JsonRequired] ImmutableHashSet<long> CollectedInstanceIds,
    [property: JsonRequired] ImmutableHashSet<long> CountedScatterInstanceIds,
    [property: JsonRequired] long SpinPayoutUnits,
    [property: JsonRequired] long RoundPayoutUnits,
    [property: JsonRequired] long PendingFreeSpins);
