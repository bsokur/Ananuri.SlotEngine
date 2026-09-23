using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Multipliers;

/// <summary>Declares bounded multiplier behavior and computes multiplier state from grid evidence.</summary>
public interface IMultiplierStrategy : IGameComponent
{
    /// <summary>All symbols this strategy can collect; empty for strategies without collection.</summary>
    ImmutableHashSet<SymbolId> CollectionSymbols { get; }
    /// <summary>Positive initial multiplier and lower bound for multiplier state.</summary>
    long Start { get; }
    /// <summary>Inclusive upper bound for both applied and retained multiplier values.</summary>
    long Maximum { get; }
    /// <summary>Whether state resets per spin or continues across a bonus.</summary>
    MultiplierPersistence Persistence { get; }
    /// <summary>Computes the current award multiplier, next state, and newly collected instances without modifying its inputs.</summary>
    MultiplierResult Apply(MultiplierState state, MultiplierContext context);
}
