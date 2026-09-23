namespace Ananuri.SlotEngine.Scatters;

/// <summary>Controls which grids may contribute previously uncounted scatter instances.</summary>
public enum ScatterTiming
{
    /// <summary>Evaluate scatter counts only on the first grid of the spin.</summary>
    InitialGrid,
    /// <summary>Evaluate each grid's previously uncounted scatter instances as an independent threshold batch.</summary>
    NewArrivalsEachGrid
}
