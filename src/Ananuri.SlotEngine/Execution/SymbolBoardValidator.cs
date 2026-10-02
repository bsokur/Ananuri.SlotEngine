using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Grid;

namespace Ananuri.SlotEngine.Execution;

internal static class SymbolBoardValidator
{
    internal static void Validate(GameDefinition game, SymbolBoard grid, long nextInstanceId)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.ReelCount != game.ReelCount || grid.RowCount != game.VisibleRows
            || grid.Cells.Any(c => c.Id >= nextInstanceId || !game.Symbols.Contains(c.Symbol)))
            throw new ArgumentException("Invalid grid dimensions, symbols, or instance sequence.");
    }
}
