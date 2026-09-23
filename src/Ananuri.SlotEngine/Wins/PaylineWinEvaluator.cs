using System.Collections.Immutable;
using System.Numerics;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;
using Ananuri.SlotEngine.Wilds;

namespace Ananuri.SlotEngine.Wins;

/// <summary>Builds left-to-right payline award candidates and selects at most one unchanged award per line.</summary>
public sealed class PaylineWinEvaluator : IWinEvaluator
{
    public PaylineWinEvaluator(ISymbolSubstitutionPolicy substitution, IAwardSelectionPolicy selection)
    {
        Substitution = substitution ?? throw new ArgumentNullException(nameof(substitution));
        Selection = selection ?? throw new ArgumentNullException(nameof(selection));
    }
    public ISymbolSubstitutionPolicy Substitution { get; }
    public IAwardSelectionPolicy Selection { get; }
    public string ConfigurationKey => $"payline-candidates-v1[{Substitution.ConfigurationKey}][{Selection.ConfigurationKey}]";
    public ImmutableHashSet<SymbolId> GetPayingSymbols(GameDefinition game) =>
        game.Paytable.Select(entry => entry.Symbol).ToImmutableHashSet();
    public ImmutableHashSet<SymbolId> SubstitutionSymbols => Substitution.InvolvedSymbols;
    public BigInteger MaximumBasePayout(GameDefinition game, long calculationStake) =>
        (BigInteger)calculationStake * game.Paytable.Max(entry => entry.Multiplier);
    public void Validate(GameDefinition game)
    {
        if (game.Paylines.IsEmpty || game.Paytable.IsEmpty)
            throw new ArgumentException("Payline evaluation requires paylines and paytable entries.");
        if (game.Bets.AllowedTotalStakeUnits.Any(stake => stake % game.Paylines.Length != 0))
            throw new ArgumentException("Each total stake must divide evenly across fixed paylines.");
        Substitution.Validate(game);
        Selection.Validate(game);
    }
    public ImmutableArray<WinAward> Evaluate(GameDefinition game, SymbolBoard grid, long calculationStake)
    {
        var wins = ImmutableArray.CreateBuilder<WinAward>();
        long lineStake = calculationStake / game.Paylines.Length;
        var candidates = ImmutableArray.CreateBuilder<WinAward>();
        foreach (var line in game.Paylines)
        {
            candidates.Clear();
            foreach (var target in game.PayingSymbols)
            {
                int count = 0;
                while (count < grid.ReelCount)
                {
                    var actual = grid[count, line.Rows[count]].Symbol;
                    if (actual != target)
                    {
                        if (!Substitution.CanSubstitute(actual, target)) break;
                        if (!SubstitutionSymbols.Contains(actual) || !SubstitutionSymbols.Contains(target))
                            throw new InvalidOperationException("Substitution policy used an undeclared symbol.");
                    }
                    count++;
                }
                var award = game.FindAward(target, count);
                if (award is null) continue;
                var positions = Enumerable.Range(0, award.MatchCount)
                    .Select(reel => new GridPosition(reel, line.Rows[reel])).ToImmutableArray();
                candidates.Add(new WinAward(line.Id, target, award.MatchCount, award.Multiplier,
                    checked(lineStake * award.Multiplier), positions,
                    positions.Where(p => grid[p.Reel, p.Row].Symbol != target).ToImmutableArray()));
            }
            var selected = Selection.Select(candidates.ToImmutable());
            if (selected is not null)
            {
                if (!candidates.Contains(selected))
                    throw new InvalidOperationException("Award selection policy must return an unchanged candidate or null.");
                wins.Add(selected);
            }
        }
        return wins.ToImmutable();
    }
}
