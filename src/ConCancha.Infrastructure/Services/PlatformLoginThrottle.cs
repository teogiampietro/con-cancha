using System.Collections.Concurrent;

namespace ConCancha.Infrastructure.Services;

/// <summary>
/// Blocks an address after too many failed platform sign-ins. In memory on purpose: a restart clears it, which is
/// acceptable for a single admin. Per address, so nobody can lock the admin out on purpose from elsewhere.
/// </summary>
public sealed class PlatformLoginThrottle(TimeProvider clock)
{
    private const int MaxFailures = 5;
    private const int MaxTrackedAddresses = 1000;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new();

    /// <returns>How long the address stays blocked, or <c>null</c> when it may try.</returns>
    public TimeSpan? RetryAfter(string address)
    {
        if (!_attempts.TryGetValue(address, out var attempts) || attempts.BlockedUntil is not { } until) return null;

        var remaining = until - clock.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public void RecordFailure(string address)
    {
        var now = clock.GetUtcNow();
        if (_attempts.Count > MaxTrackedAddresses) Forget(now);
        _attempts.AddOrUpdate(address, _ => Start(now), (_, current) => Next(current, now));
    }

    public void Clear(string address) => _attempts.TryRemove(address, out _);

    private static Attempts Start(DateTimeOffset now) => new(now, 1, null);

    private static Attempts Next(Attempts current, DateTimeOffset now)
    {
        if (now - current.WindowStart >= FailureWindow) return Start(now);

        var failures = current.Failures + 1;
        return failures < MaxFailures ? current with { Failures = failures } : new Attempts(now, 0, now + BlockDuration);
    }

    private void Forget(DateTimeOffset now)
    {
        foreach (var (address, attempts) in _attempts)
        {
            if (attempts.BlockedUntil is null && now - attempts.WindowStart >= FailureWindow)
                _attempts.TryRemove(address, out _);
        }
    }

    private sealed record Attempts(DateTimeOffset WindowStart, int Failures, DateTimeOffset? BlockedUntil);
}
