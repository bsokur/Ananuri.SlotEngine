using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Statistics;

internal sealed class PaylineStatistics
{
    internal long Observations { get; private set; }
    internal long Hits { get; private set; }
    internal decimal TotalStake { get; private set; }
    internal decimal TotalPayout { get; private set; }
    internal SortedDictionary<long, long> Distribution { get; } = [];
    internal decimal RtpPercent => TotalStake > 0 ? 100m * TotalPayout / TotalStake : 0;
    internal decimal HitPercent => Observations > 0 ? 100m * Hits / Observations : 0;

    internal void Observe(SpinEvaluation result)
    {
        Observations++;
        TotalStake += result.ChargedStakeUnits;
        TotalPayout += result.TotalPayoutUnits;
        if (result.TotalPayoutUnits > 0) Hits++;
        Distribution[result.TotalPayoutUnits] = Distribution.GetValueOrDefault(result.TotalPayoutUnits) + 1;
    }
}
