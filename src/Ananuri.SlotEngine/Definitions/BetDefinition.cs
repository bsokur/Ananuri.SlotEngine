using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Definitions;

/// <summary>An immutable set of supported total stakes, expressed in the host's integer accounting unit.</summary>
public sealed class BetDefinition
{
    private readonly FrozenSet<long> _allowed;

    /// <summary>Captures a nonempty set of unique positive total stakes and sorts it for deterministic serialization.</summary>
    public BetDefinition(IEnumerable<long> allowedTotalStakeUnits)
    {
        ArgumentNullException.ThrowIfNull(allowedTotalStakeUnits);
        var stakes = allowedTotalStakeUnits.ToArray();
        if (stakes.Length == 0 || stakes.Any(stake => stake <= 0))
            throw new ArgumentException("Provide positive allowed stakes.", nameof(allowedTotalStakeUnits));
        if (stakes.Distinct().Count() != stakes.Length)
            throw new ArgumentException("Allowed stakes must be unique.", nameof(allowedTotalStakeUnits));
        AllowedTotalStakeUnits = stakes.Order().ToImmutableArray();
        _allowed = stakes.ToFrozenSet();
    }

    /// <summary>Supported total stakes in ascending order, using the host's integer accounting unit.</summary>
    public ImmutableArray<long> AllowedTotalStakeUnits { get; }
    public bool Allows(long totalStakeUnits) => _allowed.Contains(totalStakeUnits);
}
