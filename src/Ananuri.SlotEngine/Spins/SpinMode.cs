namespace Ananuri.SlotEngine.Spins;

public enum SpinMode
{
    /// <summary>A spin that charges the calculation stake and starts a new round.</summary>
    Paid,
    /// <summary>A spin that consumes bonus state without charging a new stake.</summary>
    Free
}
