using ConCancha.Infrastructure.Services;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Infrastructure;

public class PlatformLoginThrottleTests
{
    private const string Address = "203.0.113.7";
    private const int MaxFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ControllableClock _clock = new();

    [Fact]
    public void TheFifthFailureBlocksTheAddressForFifteenMinutes()
    {
        var throttle = new PlatformLoginThrottle(_clock);
        RecordFailures(throttle, MaxFailures - 1);
        Assert.Null(throttle.RetryAfter(Address));

        throttle.RecordFailure(Address);

        var retryAfter = throttle.RetryAfter(Address);
        Assert.True(retryAfter is { } wait && wait <= Window && wait > Window - TimeSpan.FromSeconds(1));
        _clock.Advance(Window - TimeSpan.FromSeconds(1));
        Assert.NotNull(throttle.RetryAfter(Address));
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(throttle.RetryAfter(Address));
    }

    [Fact]
    public void FailuresOutsideTheWindowDoNotAddUp()
    {
        var throttle = new PlatformLoginThrottle(_clock);
        RecordFailures(throttle, MaxFailures - 1);
        _clock.Advance(Window + TimeSpan.FromMinutes(1));

        throttle.RecordFailure(Address);

        Assert.Null(throttle.RetryAfter(Address));
    }

    [Fact]
    public void AnotherAddressIsNotBlocked()
    {
        var throttle = new PlatformLoginThrottle(_clock);
        RecordFailures(throttle, MaxFailures);

        Assert.Null(throttle.RetryAfter("198.51.100.1"));
    }

    [Fact]
    public void ClearForgetsTheFailuresOfAnAddress()
    {
        var throttle = new PlatformLoginThrottle(_clock);
        RecordFailures(throttle, MaxFailures - 1);

        throttle.Clear(Address);
        throttle.RecordFailure(Address);

        Assert.Null(throttle.RetryAfter(Address));
    }

    private void RecordFailures(PlatformLoginThrottle throttle, int count)
    {
        for (var attempt = 0; attempt < count; attempt++)
            throttle.RecordFailure(Address);
    }
}
