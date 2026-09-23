using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Immutable reel-major grid. Instance IDs survive gravity and identify collections.</summary>
public sealed class SymbolBoard
{
    public SymbolBoard(int reelCount, int rowCount, IEnumerable<SymbolInstance> cells)
        : this(reelCount, rowCount, (cells ?? throw new ArgumentNullException(nameof(cells))).ToImmutableArray()) { }

    /// <summary>Captures a rectangular board with positive dimensions and unique non-negative instance IDs in reel-major order.</summary>
    [JsonConstructor]
    public SymbolBoard(int reelCount, int rowCount, ImmutableArray<SymbolInstance> cells)
    {
        if (cells.IsDefault) throw new ArgumentException("Grid cells are required.", nameof(cells));
        if (reelCount <= 0 || rowCount <= 0 || (long)reelCount * rowCount > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(reelCount));
        Cells = cells.ToImmutableArray();
        if (Cells.Length != reelCount * rowCount || Cells.Any(c => c is null || c.Id < 0)
            || Cells.Select(c => c.Id).Distinct().Count() != Cells.Length)
            throw new ArgumentException("Grid cells must have the correct dimensions and unique non-negative instance IDs.", nameof(cells));
        ReelCount = reelCount;
        RowCount = rowCount;
    }

    public int ReelCount { get; }
    public int RowCount { get; }
    /// <summary>Immutable cells in reel-major order: reel * RowCount + row.</summary>
    public ImmutableArray<SymbolInstance> Cells { get; }
    public SymbolInstance this[int reel, int row]
    {
        get
        {
            if (reel < 0 || reel >= ReelCount) throw new ArgumentOutOfRangeException(nameof(reel));
            if (row < 0 || row >= RowCount) throw new ArgumentOutOfRangeException(nameof(row));
            return Cells[reel * RowCount + row];
        }
    }
}
