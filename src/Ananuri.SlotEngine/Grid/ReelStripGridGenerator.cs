using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Builds a board from one uniform initial stop per reel, using the reels configured for the spin mode.</summary>
public sealed class ReelStripGridGenerator : IGridGenerator
{
    public string ConfigurationKey => "uniform-reel-stops-v1";
    public void Validate(GameDefinition game) { }
    public GeneratedGrid Generate(GameDefinition game, SpinMode mode, DrawSequence draws)
    {
        var reels = mode == SpinMode.Free ? game.FreeSpinReels : game.Reels;
        var cells = new List<SymbolInstance>();
        var stops = ImmutableArray.CreateBuilder<int>();
        long id = 0;
        for (int reel = 0; reel < reels.Length; reel++)
        {
            int index = draws.Next($"initial/reel/{reel}", reels[reel].Length);
            stops.Add(index);
            for (int row = 0; row < game.VisibleRows; row++)
            {
                cells.Add(new(id++, reels[reel][index]));
                index++;
                if (index == reels[reel].Length) index = 0;
            }
        }
        return new(new SymbolBoard(reels.Length, game.VisibleRows, cells), stops.ToImmutable(), id);
    }
}
