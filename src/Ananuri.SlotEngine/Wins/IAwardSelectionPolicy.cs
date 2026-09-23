using System.Collections.Immutable;
using Ananuri.SlotEngine.Configuration;

namespace Ananuri.SlotEngine.Wins;

/// <summary>Selects at most one award from the valid candidates for a payline.</summary>
public interface IAwardSelectionPolicy : IGameComponent
{
    /// <summary>Selects one of the supplied candidates unchanged, or null to award nothing.</summary>
    /// <remarks>The engine rejects invented awards and changes to any candidate field.</remarks>
    /// <param name="candidates">Valid awards for one payline; may be empty.</param>
    WinAward? Select(ImmutableArray<WinAward> candidates);
}
