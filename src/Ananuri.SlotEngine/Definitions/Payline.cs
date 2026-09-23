using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Definitions;

/// <summary>An immutable left-to-right path selecting one row on each reel.</summary>
public sealed class Payline
{
    public Payline(int id, IEnumerable<int> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
        Rows = rows.ToImmutableArray();
        if (Rows.IsEmpty || Rows.Any(row => row < 0))
            throw new ArgumentException("Rows must be non-empty and non-negative.", nameof(rows));
        Id = id;
    }

    /// <summary>Non-negative line identifier, unique within the game.</summary>
    public int Id { get; }
    /// <summary>Zero-based row selected on each reel, ordered left to right.</summary>
    public ImmutableArray<int> Rows { get; }
}
