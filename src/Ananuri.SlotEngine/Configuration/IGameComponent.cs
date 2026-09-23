using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Configuration;

/// <summary>
/// Implementations must be immutable and deterministic. ConfigurationKey includes the implementation
/// version and all behavior parameters, including symbol roles, capabilities, and payout bounds.
/// Declarations must cover every possible output, not just outcomes observed so far.
/// </summary>
public interface IGameComponent
{
    /// <summary>Stable identifier including implementation version and every setting that can affect evaluation.</summary>
    string ConfigurationKey { get; }
    /// <summary>Checks whether this component is compatible with the supplied game; throws ArgumentException for invalid configuration.</summary>
    void Validate(GameDefinition game);
}
