using System.Collections.Immutable;
using Ananuri.SlotEngine.Definitions;
using Ananuri.SlotEngine.Spins;

namespace Ananuri.SlotEngine.Randomness;

/// <summary>
/// Supplies a finite set of explicit initial reel stops for the selected spin mode. Requests must match
/// each reel's ordinal, initial-draw purpose, and reel length. Refills and mismatched requests fail;
/// use a source that also supplies refills when a game can cascade. The same stops can be reused across evaluations.
/// </summary>
public sealed class FixedReelStops : IRandomDrawSource
{
    private readonly ImmutableArray<int> _stops;
    private readonly ImmutableArray<int> _bounds;

    /// <summary>Captures exactly one valid zero-based stop per reel for the chosen spin mode.</summary>
    public FixedReelStops(GameDefinition game, IEnumerable<int> stops, SpinMode mode = SpinMode.Paid)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(stops);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var reels = mode == SpinMode.Free ? game.FreeSpinReels : game.Reels;
        _stops = stops.ToImmutableArray();
        _bounds = reels.Select(reel => reel.Length).ToImmutableArray();
        if (_stops.Length != reels.Length)
            throw new ArgumentException("Exactly one stop per reel is required.", nameof(stops));
        for (int reel = 0; reel < reels.Length; reel++)
            if (_stops[reel] < 0 || _stops[reel] >= _bounds[reel])
                throw new ArgumentOutOfRangeException(nameof(stops), $"Invalid stop for reel {reel}.");
    }

    public int Next(DrawRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ordinal < 0 || request.Ordinal >= _stops.Length)
            throw new InvalidOperationException("Fixed reel stops supply initial draws only; provide a refill-capable source for cascades.");
        int reel = (int)request.Ordinal;
        if (request.Purpose != $"initial/reel/{reel}" || request.ExclusiveUpperBound != _bounds[reel])
            throw new InvalidOperationException("Draw request does not match the configured initial reel stop.");
        return _stops[reel];
    }
}
