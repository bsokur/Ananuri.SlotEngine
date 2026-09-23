using System.Collections.Immutable;

namespace Ananuri.SlotEngine.Randomness;

/// <summary>In-memory recording/memoization for tools and tests; NOT a durable production allocation store.</summary>
/// <param name="source">Underlying draw allocator; recorded values are reused for repeated identical requests.</param>
public sealed class RecordingDrawSource(IRandomDrawSource source) : IRandomDrawSource
{
    private readonly IRandomDrawSource _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly Dictionary<(string, long), RecordedDraw> _draws = [];
    private readonly object _gate = new();
    /// <summary>Returns an immutable snapshot of allocated draws. Consumers should identify draws by evaluation ID and ordinal.</summary>
    public ImmutableArray<RecordedDraw> Snapshot()
    {
        lock (_gate) return _draws.Values.ToImmutableArray();
    }
    public int Next(DrawRequest request)
    {
        lock (_gate)
        {
            var key = (request.EvaluationId, request.Ordinal);
            if (_draws.TryGetValue(key, out var recorded))
            {
                if (recorded.Request != request) throw new InvalidOperationException("Replay draw request changed.");
                return recorded.Value;
            }
            int value = _source.Next(request);
            if (value < 0 || value >= request.ExclusiveUpperBound)
                throw new InvalidOperationException("Random source returned an out-of-range value.");
            _draws.Add(key, new RecordedDraw(request, value));
            return value;
        }
    }
}
