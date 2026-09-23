using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.FreeSpins;

/// <summary>Disables free spins and rejects bonus requests or pending grants.</summary>
public sealed class NoFreeSpins : IFeaturePolicy
{
    public bool SupportsFreeSpins => false;
    public string ConfigurationKey => "no-free-spins-v1";
    public void Validate(GameDefinition game) { }
    public void ValidateState(GameDefinition game, BonusState state) => throw new ArgumentException("This game has no free-spin feature.");
    public FeatureTransition Complete(GameDefinition game, SpinRequest request, long pendingFreeSpins,
        long multiplier, long roundPayout, bool winLimitReached)
    {
        if (pendingFreeSpins != 0) throw new InvalidOperationException("Scatter requested a disabled feature.");
        return new(null, 0);
    }
}
