namespace Ananuri.SlotEngine.Randomness;

/// <summary>Assigns increasing ordinals to draw requests and rejects values outside their requested bounds.</summary>
public sealed class DrawSequence
{
    private readonly IRandomDrawSource _source;
    /// <summary>Binds an evaluation to its draw source and initial non-negative ordinal; use the saved ordinal when resuming.</summary>
    public DrawSequence(string evaluationId, IRandomDrawSource source, long nextOrdinal = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(source);
        if (nextOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(nextOrdinal));
        EvaluationId = evaluationId;
        _source = source;
        NextOrdinal = nextOrdinal;
    }
    public string EvaluationId { get; }
    public long NextOrdinal { get; private set; }
    /// <summary>Requests an integer in [0, upperBound), validates the result, and advances the ordinal after a successful draw.</summary>
    public int Next(string purpose, int upperBound)
    {
        if (upperBound <= 0) throw new ArgumentOutOfRangeException(nameof(upperBound));
        var request = new DrawRequest(EvaluationId, NextOrdinal, purpose, upperBound);
        int value = _source.Next(request);
        if (value < 0 || value >= upperBound)
            throw new InvalidOperationException($"Draw {NextOrdinal} is outside its requested range.");
        NextOrdinal++;
        return value;
    }
}
