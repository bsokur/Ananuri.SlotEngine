using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Randomness;

/// <summary>
/// Supplies only the captured draw requests and values. Evaluation ID, ordinal, purpose, and bound
/// must match the recording exactly. Missing or mismatched requests fail without allocating fresh draws.
/// </summary>
public sealed class ReplayDrawSource : IRandomDrawSource
{
    private readonly ImmutableDictionary<(string, long), RecordedDraw> _draws;
    /// <summary>Captures valid recorded draws indexed by evaluation ID and ordinal; duplicate identities are rejected.</summary>
    public ReplayDrawSource(IEnumerable<RecordedDraw> draws)
    {
        ArgumentNullException.ThrowIfNull(draws);
        var captured = draws.ToArray();
        if (captured.Any(d => d is null || d.Request is null || string.IsNullOrWhiteSpace(d.Request.EvaluationId)
            || d.Request.Ordinal < 0 || d.Request.ExclusiveUpperBound <= 0
            || d.Value < 0 || d.Value >= d.Request.ExclusiveUpperBound))
            throw new ArgumentException("Invalid recorded draw.", nameof(draws));
        _draws = captured.ToImmutableDictionary(d => (d.Request.EvaluationId, d.Request.Ordinal));
    }
    public int Next(DrawRequest request)
    {
        if (!_draws.TryGetValue((request.EvaluationId, request.Ordinal), out var draw)
            || draw.Request != request)
            throw new InvalidOperationException($"Missing or mismatched replay draw {request.Ordinal}.");
        return draw.Value;
    }
}
