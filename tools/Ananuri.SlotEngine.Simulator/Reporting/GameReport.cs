using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Simulator.Commands;
using Ananuri.SlotEngine.Simulator.Statistics;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Reporting;

internal static class GameReport
{
    internal static void PrintSummary(GameDefinition game, GameSimulationOptions options,
        GameStatistics statistics, long stake, TimeSpan elapsed)
    {
        bool demo = options.Demo;
        int count = statistics.CompletedRounds, seed = options.Seed;
        Console.WriteLine($"Game: {game.GameId}; math: {game.MathVersion}; fingerprint: {game.Fingerprint}");
        Console.WriteLine($"Engine: {SlotEngine.EngineVersion}; rules: {SlotEngine.RulesVersion}");
        Console.WriteLine($"Offline {(demo ? "scripted initial grid" : "uniform initial stops")}; seed {seed}; runtime {Environment.Version}. Experimental math.");
        Console.WriteLine($"Paid rounds: {count}; free spins: {statistics.FreeSpins}; bonus rounds: {statistics.BonusRounds}; retriggers: {statistics.Retriggers}");
        Console.WriteLine($"Charged stake: {statistics.PaidStake}; base payout: {statistics.BasePayout}; bonus payout: {statistics.BonusPayout}");
        Console.WriteLine($"{(demo ? "Scripted payout/stake ratio (not an RTP estimate)" : "Round RTP")}: {statistics.RtpPercent:F6}%");
        Console.WriteLine($"Base contribution: {statistics.BaseContributionPercent:F6}%; bonus contribution: {statistics.BonusContributionPercent:F6}%");
        Console.WriteLine($"Shares of total payout: base {statistics.BasePayoutSharePercent:F6}%; bonus {statistics.BonusPayoutSharePercent:F6}%");
        Console.WriteLine($"Round hit frequency: {statistics.RoundHitPercent:F6}%; bonus frequency: {statistics.BonusFrequencyPercent:F6}%");
        if (count > 1)
        {
            double standardError = Math.Sqrt(statistics.SampleVariance / count);
            Console.WriteLine($"Round payout standard deviation: {Math.Sqrt(statistics.SampleVariance):F6}x stake");
            Console.WriteLine($"Approximate 95% RTP interval (independent complete rounds): {100 * (statistics.Mean - 1.96 * standardError):F4}% to {100 * (statistics.Mean + 1.96 * standardError):F4}%");
        }
        Console.WriteLine($"Refills: {statistics.Cascades}; largest observed multiplier: {statistics.MaxMultiplier}x; longest bonus: {statistics.LongestBonus}; cap hits: {statistics.CapHits}");
        Console.WriteLine($"Largest observed round: {statistics.LargestRound}; configured round limit: {game.WinLimit.MaximumPayout(stake)?.ToString() ?? "none"}");
        string[] labels = ["zero", "0-1x (exclusive)", "1-2x", "2-10x", "10-100x", "100x+"];
        for (int i = 0; i < statistics.Bands.Length; i++) Console.WriteLine($"Payout band {labels[i]}: {statistics.Bands[i]} rounds");
        Console.WriteLine($"Refills per spin (count:frequency): {string.Join(", ", statistics.CascadeDistribution.Select(p => $"{p.Key}:{p.Value}"))}");
        Console.WriteLine($"Bonus lengths (spins:frequency): {string.Join(", ", statistics.BonusLengths.Select(p => $"{p.Key}:{p.Value}"))}");
        Console.WriteLine($"Applied multipliers (value:grid-count): {string.Join(", ", statistics.MultiplierDistribution.Select(p => $"{p.Key}:{p.Value}"))}");
        Console.WriteLine($"Processed in: {elapsed.TotalMilliseconds:F3} ms");
    }

    internal static void PrintSpin(SpinEvaluation result)
    {
        Console.WriteLine($"{result.EvaluationId}: {result.Mode}; charged {result.ChargedStakeUnits}; payout {result.TotalPayoutUnits}; new free spins {result.GrantedFreeSpins}");
        foreach (var step in result.Steps)
            Console.WriteLine($"  Grid {step.GridIndex}: lines {step.Awards.Length}; scatters {step.Scatter.Count}; multiplier {step.MultiplierBefore}->{step.MultiplierAfter}; paid {step.PayablePayoutUnits}; removed {step.Transition?.Removed.Length ?? 0}");
    }
}
