using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Definitions.Validation;

internal static class GameDefinitionValidator
{
    public static void Validate(GameDefinition game)
    {
        if (string.IsNullOrWhiteSpace(game.GameId)) Invalid("gameId", "is required");
        if (string.IsNullOrWhiteSpace(game.MathVersion)) Invalid("mathVersion", "is required");
        if (game.VisibleRows <= 0) Invalid("visibleRows", "must be positive");
        if (game.Symbols.IsEmpty || game.Symbols.Any(s => s.Value < 0))
            Invalid("symbols", "must contain non-negative IDs");
        if (game.Symbols.Distinct().Count() != game.Symbols.Length)
            Invalid("symbols", "contains duplicate IDs");
        if (game.Reels.IsEmpty || game.Reels.Any(r => r is null))
            Invalid("reels", "must contain non-null reels");
        if (game.Paylines.Any(p => p is null))
            Invalid("paylines", "must not contain null paylines");
        if (game.Paytable.Any(p => p is null))
            Invalid("paytable", "must not contain null entries");

        if ((long)game.ReelCount * game.VisibleRows > int.MaxValue)
            Invalid("visibleRows", "grid dimensions exceed indexing capacity");

        var known = game.Symbols.ToHashSet();
        for (int r = 0; r < game.ReelCount; r++)
        {
            if (game.Reels[r].Symbols.Any(s => !known.Contains(s)))
                Invalid($"reels[{r}]", "contains an undeclared symbol");
        }

        var ids = new HashSet<int>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in game.Paylines)
        {
            if (!ids.Add(line.Id)) Invalid("paylines", "contains duplicate IDs");
            if (line.Rows.Length != game.ReelCount || line.Rows.Any(row => row >= game.VisibleRows))
                Invalid($"paylines[{line.Id}]", "must select a valid row on every reel");
            if (!paths.Add(string.Join(",", line.Rows)))
                Invalid("paylines", "contains duplicate paths");
        }

        var keys = new HashSet<(SymbolId, int)>();
        foreach (var award in game.Paytable)
        {
            if (!known.Contains(award.Symbol) || award.MatchCount > game.ReelCount)
                Invalid("paytable", "contains an unknown symbol or impossible match length");
            if (!keys.Add((award.Symbol, award.MatchCount)))
                Invalid("paytable", "contains duplicate symbol/length entries");
        }

        foreach (var group in game.Paytable.GroupBy(p => p.Symbol))
        {
            long previous = 0;
            foreach (var award in group.OrderBy(p => p.MatchCount))
            {
                if (award.Multiplier < previous)
                    Invalid("paytable", "multipliers must not decrease as matches get longer");
                previous = award.Multiplier;
            }
        }

    }

    private static void Invalid(string path, string message) =>
        throw new ArgumentException($"Invalid game definition: {path} {message}.", path);
}
