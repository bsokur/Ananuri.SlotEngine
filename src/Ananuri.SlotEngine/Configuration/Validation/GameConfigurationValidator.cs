using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Multipliers;

namespace Ananuri.SlotEngine.Configuration.Validation;

internal static class GameConfigurationValidator
{
    internal static void Validate(GameDefinition game)
    {
        foreach (var component in game.Components)
        {
            if (string.IsNullOrWhiteSpace(component.ConfigurationKey)) throw new ArgumentException("Components require a versioned configuration key.");
            component.Validate(game);
        }
        if (game.FreeSpinReels.Length != game.ReelCount || game.FreeSpinReels.Any(r => r is null || r.Symbols.Any(s => !game.Symbols.Contains(s))))
            throw new ArgumentException("Free-spin reels must match the dimensions and declared symbols.");
        if (game.PaidMultiplier.Persistence != MultiplierPersistence.Spin)
            throw new ArgumentException("Paid-spin multiplier state must end with the paid spin.");
        if (new[] { game.PaidMultiplier, game.BonusMultiplier }.Any(m => m.Start <= 0 || m.Maximum < m.Start || !Enum.IsDefined(m.Persistence)))
            throw new ArgumentException("Invalid multiplier bounds.");

        var paying = DeclaredSymbols(game, game.PayingSymbols.ToImmutableHashSet(), "paying");
        var substitutions = DeclaredSymbols(game, game.Wins.SubstitutionSymbols, "substitution");
        var scatters = DeclaredSymbols(game, game.Scatters.Symbols, "scatter");
        var collections = DeclaredSymbols(game, game.PaidMultiplier.CollectionSymbols, "paid collection")
            .Union(DeclaredSymbols(game, game.BonusMultiplier.CollectionSymbols, "bonus collection"));
        var refills = DeclaredSymbols(game, game.Cascades.RefillSymbols, "refill");
        if (scatters.Overlaps(collections))
            throw new ArgumentException("Scatter and collection roles must be distinct.");
        var restricted = scatters.Union(collections);
        if (paying.Overlaps(restricted))
            throw new ArgumentException("Scatter and collection symbols must not have ordinary awards.");
        if (substitutions.Overlaps(restricted))
            throw new ArgumentException("Wilds cannot substitute for scatter or collection symbols, or share their roles.");
        if (game.Scatters.CanAwardFreeSpins && !game.Features.SupportsFreeSpins)
            throw new ArgumentException("Scatter triggers require a free-spin feature.");
        if (!game.AllowScatterRefills && refills.Overlaps(scatters))
            throw new ArgumentException("Scatter refill symbols require explicit opt-in; select their evaluation timing separately.");

        long scatterMultiplier = game.Scatters.MaximumAwardMultiplier;
        if (scatterMultiplier < 0) throw new ArgumentException("Scatter award bound must be non-negative.");
        long maximumMultiplier = Math.Max(game.PaidMultiplier.Maximum, game.BonusMultiplier.Maximum);
        foreach (long stake in game.Bets.AllowedTotalStakeUnits)
        {
            if (game.WinLimit.MaximumPayout(stake) is <= 0)
                throw new ArgumentException("A configured win limit must be positive or null for no cap.");
            BigInteger ordinaryBound = game.Wins.MaximumBasePayout(game, stake);
            if (ordinaryBound < 0) throw new ArgumentException("Ordinary award bound must be non-negative.");
            BigInteger bound = ordinaryBound * maximumMultiplier
                + (BigInteger)stake * scatterMultiplier * (game.MultiplyScatterAwards ? maximumMultiplier : 1);
            if (bound > long.MaxValue) throw new ArgumentException("A grid's raw award can exceed payout storage.");
        }
    }

    private static ImmutableHashSet<SymbolId> DeclaredSymbols(GameDefinition game, ImmutableHashSet<SymbolId> symbols, string role)
    {
        if (symbols is null || symbols.Any(symbol => !game.Symbols.Contains(symbol)))
            throw new ArgumentException($"The {role} role must declare a set of known symbols.");
        return symbols;
    }
}
