namespace Ananuri.SlotEngine.Randomness;

/// <summary>
/// Supplies values for draw requests. The runtime owns random allocation and durable replay.
/// A source may support only a finite, predetermined set of requests, such as fixed initial stops
/// or a recorded evaluation. Choose a source that covers every draw the selected game can request.
/// </summary>
public interface IRandomDrawSource
{
    /// <summary>Returns a value in [0, request.ExclusiveUpperBound) for a supported request.</summary>
    /// <exception cref="InvalidOperationException">
    /// The request is unavailable or does not match the source's predetermined draw. Such a source
    /// must fail rather than replace missing or mismatched values with fresh draws.
    /// </exception>
    int Next(DrawRequest request);
}
