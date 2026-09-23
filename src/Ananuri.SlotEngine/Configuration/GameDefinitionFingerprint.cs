using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ananuri.SlotEngine.Definitions;

namespace Ananuri.SlotEngine.Configuration;

internal static class GameDefinitionFingerprint
{
    internal static string Compute(GameDefinition game)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            game.GameId,
            game.MathVersion,
            game.VisibleRows,
            Symbols = game.Symbols.Select(s => s.Value),
            Reels = game.Reels.Select(r => r.Symbols.Select(s => s.Value)),
            FreeReels = game.FreeSpinReels.Select(r => r.Symbols.Select(s => s.Value)),
            Lines = game.Paylines.Select(p => new { p.Id, p.Rows }),
            Paytable = game.Paytable.Select(p => new { Symbol = p.Symbol.Value, p.MatchCount, p.Multiplier }),
            Stakes = game.Bets.AllowedTotalStakeUnits,
            Components = game.Components.Select(c => c.ConfigurationKey),
            game.MultiplyScatterAwards,
            game.AllowScatterRefills,
            Rules = SlotEngine.RulesVersion
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
