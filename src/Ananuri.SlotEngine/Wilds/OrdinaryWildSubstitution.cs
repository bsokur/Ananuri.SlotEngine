using System.Collections.Frozen;
using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Wilds;

/// <summary>Allows each configured wild to substitute for each configured ordinary paying target.</summary>
public sealed class OrdinaryWildSubstitution : ISymbolSubstitutionPolicy
{
    /// <summary>Creates substitution rules from disjoint wild and target symbol sets.</summary>
    /// <param name="wilds">Actual symbols that may substitute; an empty set disables substitution.</param>
    /// <param name="targets">Ordinary paying symbols that the wilds may represent.</param>
    /// <exception cref="ArgumentException">A symbol is declared as both a wild and a target.</exception>
    public OrdinaryWildSubstitution(IEnumerable<SymbolId> wilds, IEnumerable<SymbolId> targets)
    {
        ArgumentNullException.ThrowIfNull(wilds);
        ArgumentNullException.ThrowIfNull(targets);
        Wilds = wilds.ToFrozenSet();
        Targets = targets.ToFrozenSet();
        if (Wilds.Overlaps(Targets)) throw new ArgumentException("Wild symbols cannot be substitution targets.");
        InvolvedSymbols = Wilds.Concat(Targets).ToImmutableHashSet();
    }
    public FrozenSet<SymbolId> Wilds { get; }
    public FrozenSet<SymbolId> Targets { get; }
    public ImmutableHashSet<SymbolId> InvolvedSymbols { get; }
    public string ConfigurationKey => $"ordinary-wild-v1:{string.Join(',', Wilds.OrderBy(s => s.Value).Select(s => s.Value))}:{string.Join(',', Targets.OrderBy(s => s.Value).Select(s => s.Value))}";
    public bool CanSubstitute(SymbolId actual, SymbolId target) => Wilds.Contains(actual) && Targets.Contains(target);
    public void Validate(GameDefinition game)
    {
        if (Wilds.Concat(Targets).Any(s => !game.Symbols.Contains(s)))
            throw new ArgumentException("Wild configuration references an unknown symbol.");
    }
}
