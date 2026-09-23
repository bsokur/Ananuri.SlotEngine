using Ananuri.SlotEngine.Configuration;
using Ananuri.SlotEngine.Samples;
using Xunit;

namespace Ananuri.SlotEngine.Tests.Configuration;

public sealed class TutorialTests
{
    [Fact]
    public void Tutorial_ShouldMatchTheDocumentedRoundAndReplay()
    {
        using var stream = typeof(TutorialTests).Assembly.GetManifestResourceStream("FirstGame.json")!;
        using var reader = new StreamReader(stream);
        var game = GamePackageLoader.Load(reader.ReadToEnd());
        using var output = new StringWriter();
        var spins = TutorialExample.EvaluateRound(game, output);
        Assert.Equal(new long[] { 500, 300, 600 }, spins.Select(s => s.TotalPayoutUnits));
        Assert.Equal(new long[] { 500, 800, 1400 }, spins.Select(s => s.RoundPayoutUnits));
        Assert.Equal(new long[] { 100, 0, 0 }, spins.Select(s => s.ChargedStakeUnits));
        Assert.All(spins, spin => Assert.Equal(2, spin.Steps.Length));
        Assert.Equal(new[] { 2, 1, 0 }, spins.Select(s => s.NextBonus?.RemainingSpins ?? 0));
        Assert.Equal(new long[] { 8, 1, 8, 1, 8, 1 }, spins[0].Steps[0].Grid.Cells.Select(c => (long)c.Symbol.Value));
        Assert.Equal(new long[] { 3, 8, 3, 8, 3, 8 }, spins[0].Steps[1].Grid.Cells.Select(c => (long)c.Symbol.Value));
        Assert.Contains("Replay verified: 3 evaluations; 18 draws.", output.ToString());
    }
}
