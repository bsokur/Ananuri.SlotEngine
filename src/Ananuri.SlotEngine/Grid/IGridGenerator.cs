using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Randomness;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Grid;

/// <summary>Creates an initial grid and reports the random stops and symbol identities used.</summary>
public interface IGridGenerator : IGameComponent
{
    /// <summary>Creates a full board of declared symbols with unique non-negative instance IDs below NextInstanceId and one valid stop per reel.</summary>
    GeneratedGrid Generate(GameDefinition game, SpinMode mode, DrawSequence draws);
}
