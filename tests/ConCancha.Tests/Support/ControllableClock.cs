namespace ConCancha.Tests.Support;

/// <summary>Real time plus an offset the tests move forward, to get past time-based blocks without waiting.</summary>
public sealed class ControllableClock : TimeProvider
{
    private TimeSpan _offset;

    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + _offset;

    public void Advance(TimeSpan by) => _offset += by;

    public void Reset() => _offset = TimeSpan.Zero;
}
