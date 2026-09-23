namespace Ananuri.SlotEngine.Configuration.Packages;

/// <summary>One JSON paytable entry, using a game-scoped numeric symbol identifier.</summary>
/// <param name="Symbol">Game-scoped symbol identifier.</param>
/// <param name="MatchCount">Qualifying consecutive match length from the leftmost reel.</param>
/// <param name="Multiplier">Positive multiplier of one line's share of the total stake.</param>
public sealed record AwardPackage(int Symbol, int MatchCount, long Multiplier);
