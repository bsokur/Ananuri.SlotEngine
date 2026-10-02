using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Simulator.Statistics;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Simulator.Reporting;

internal static class PaylineReport
{
    internal static void PrintSummary(GameDefinition game, PaylineStatistics statistics, string heading, TimeSpan elapsed)
    {
        Console.WriteLine(heading);
        Console.WriteLine($"Engine: {SlotEngine.EngineVersion}");
        Console.WriteLine($"Game: {game.GameId}; math: {game.MathVersion}; rules: {SlotEngine.RulesVersion}");
        Console.WriteLine($"Observations: {statistics.Observations}");
        Console.WriteLine($"Stake: {statistics.TotalStake}; payout: {statistics.TotalPayout}");
        Console.WriteLine($"RTP: {statistics.RtpPercent:F6}%");
        Console.WriteLine($"Hit frequency (any payout): {statistics.HitPercent:F6}%");
        foreach (var pair in statistics.Distribution) Console.WriteLine($"Payout {pair.Key}: {pair.Value} outcomes");
        Console.WriteLine($"Processed in: {elapsed.TotalMilliseconds:F3} ms");
    }

    internal static void PrintDemo(SpinEvaluation result, TimeSpan elapsed)
    {
        Console.WriteLine($"Game: {result.GameId} / math {result.MathVersion}");
        Console.WriteLine($"Engine: {result.EngineVersion} / rules {result.RulesVersion}");
        var grid = result.Steps[0].Grid;
        for (int row = 0; row < grid.RowCount; row++)
            Console.WriteLine(string.Join(" ", Enumerable.Range(0, grid.ReelCount).Select(reel => grid[reel, row].Symbol.Value)));
        Console.WriteLine($"Stake: {result.ChargedStakeUnits}; payout: {result.TotalPayoutUnits}");
        Console.WriteLine($"Processed in: {elapsed.TotalMilliseconds:F3} ms");
    }
}
