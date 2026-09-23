using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Statistics;

internal sealed class GameStatistics
{
    internal decimal PaidStake { get; private set; }
    internal decimal BasePayout { get; private set; }
    internal decimal BonusPayout { get; private set; }
    internal long FreeSpins { get; private set; }
    internal long BonusRounds { get; private set; }
    internal long Retriggers { get; private set; }
    internal long CapHits { get; private set; }
    internal long RoundHits { get; private set; }
    internal long Cascades { get; private set; }
    internal long MaxMultiplier { get; private set; } = 1;
    internal long LargestRound { get; private set; }
    internal long LongestBonus { get; private set; }
    internal int CompletedRounds { get; private set; }
    internal double Mean { get; private set; }
    internal double M2 { get; private set; }
    internal double SampleVariance => CompletedRounds > 1 ? M2 / (CompletedRounds - 1) : 0;
    internal decimal RtpPercent => PaidStake > 0 ? 100m * (BasePayout + BonusPayout) / PaidStake : 0;
    internal decimal BaseContributionPercent => PaidStake > 0 ? 100m * BasePayout / PaidStake : 0;
    internal decimal BonusContributionPercent => PaidStake > 0 ? 100m * BonusPayout / PaidStake : 0;
    internal decimal BasePayoutSharePercent => BasePayout + BonusPayout > 0 ? 100m * BasePayout / (BasePayout + BonusPayout) : 0;
    internal decimal BonusPayoutSharePercent => BasePayout + BonusPayout > 0 ? 100m * BonusPayout / (BasePayout + BonusPayout) : 0;
    internal decimal RoundHitPercent => CompletedRounds > 0 ? 100m * RoundHits / CompletedRounds : 0;
    internal decimal BonusFrequencyPercent => CompletedRounds > 0 ? 100m * BonusRounds / CompletedRounds : 0;
    internal SortedDictionary<int, long> CascadeDistribution { get; } = new();
    internal SortedDictionary<int, long> BonusLengths { get; } = new();
    internal SortedDictionary<long, long> MultiplierDistribution { get; } = new();
    internal long[] Bands { get; } = new long[6];

    internal void ObservePaidSpin(SpinEvaluation result)
    {
        RequireComplete(result);
        if (result.Mode != SpinMode.Paid) throw new ArgumentException("A paid-spin result is required.", nameof(result));
        PaidStake += result.ChargedStakeUnits;
        BasePayout += result.TotalPayoutUnits;
        if (result.NextBonus is not null) BonusRounds++;
        Track(result);
    }

    internal void ObserveFreeSpin(SpinEvaluation result)
    {
        RequireComplete(result);
        if (result.Mode != SpinMode.Free) throw new ArgumentException("A free-spin result is required.", nameof(result));
        if (result.ChargedStakeUnits != 0) throw new InvalidOperationException("A free spin charged stake.");
        BonusPayout += result.TotalPayoutUnits;
        FreeSpins++;
        if (result.GrantedFreeSpins > 0) Retriggers++;
        Track(result);
    }

    internal void ObserveRound(SpinEvaluation result, int bonusLength, long stake)
    {
        RequireComplete(result);
        if (result.NextBonus is not null) throw new ArgumentException("Cannot count an unfinished bonus round.", nameof(result));
        if (bonusLength < 0) throw new ArgumentOutOfRangeException(nameof(bonusLength));
        if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
        CompletedRounds++;
        if (bonusLength > 0) BonusLengths[bonusLength] = BonusLengths.GetValueOrDefault(bonusLength) + 1;
        LongestBonus = Math.Max(LongestBonus, bonusLength);
        long roundPayout = result.RoundPayoutUnits;
        if (roundPayout > 0) RoundHits++;
        if (result.CompletionReason == CompletionReason.WinLimit) CapHits++;
        LargestRound = Math.Max(LargestRound, roundPayout);
        decimal exactMultiple = (decimal)roundPayout / stake;
        double multiple = (double)exactMultiple;
        double delta = multiple - Mean;
        Mean += delta / CompletedRounds;
        M2 += delta * (multiple - Mean);
        int band = exactMultiple == 0 ? 0 : exactMultiple < 1 ? 1 : exactMultiple < 2 ? 2 : exactMultiple < 10 ? 3 : exactMultiple < 100 ? 4 : 5;
        Bands[band]++;
    }

    private static void RequireComplete(SpinEvaluation result)
    {
        if (!result.IsComplete || result.CompletionReason == CompletionReason.WorkBudget)
            throw new ArgumentException("Cannot include an unfinished spin in statistics.", nameof(result));
    }

    private void Track(SpinEvaluation evaluation)
    {
        int refills = evaluation.Steps.Count(s => s.Transition is not null);
        Cascades += refills;
        CascadeDistribution[refills] = CascadeDistribution.GetValueOrDefault(refills) + 1;
        foreach (var step in evaluation.Steps)
        {
            MaxMultiplier = Math.Max(MaxMultiplier, Math.Max(step.AppliedMultiplier, step.MultiplierAfter));
            MultiplierDistribution[step.AppliedMultiplier] = MultiplierDistribution.GetValueOrDefault(step.AppliedMultiplier) + 1;
        }
    }
}
