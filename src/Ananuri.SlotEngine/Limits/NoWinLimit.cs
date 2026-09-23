using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Limits;

/// <summary>No gameplay payout cap. Checked arithmetic still rejects storage overflow.</summary>
public sealed class NoWinLimit : IWinLimitPolicy
{
    public string ConfigurationKey => "no-win-limit-v1";
    public long? MaximumPayout(long calculationStake) => null;
    public void Validate(GameDefinition game) { }
}
