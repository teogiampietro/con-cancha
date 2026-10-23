using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Time;

namespace ConCancha.Infrastructure.Services;

/// <summary>
/// Time zone of the platform figures. Every club is in Argentina today (the country is fixed), so one zone gives
/// each club its own "today" and lets the database count all clubs in a single query.
/// </summary>
public static class PlatformReporting
{
    public static readonly ClubTimeZone Zone = new(TimeZoneInfo.FindSystemTimeZoneById(ClubDefaults.TimeZoneId));

    public static DateTimeOffset StartOf(DateOnly day) => Zone.ToUtc(day, 0);

    /// <summary>From the start of <paramref name="fromDay"/> to the start of <paramref name="toDay"/>, in UTC.</summary>
    public static (DateTimeOffset From, DateTimeOffset To) UtcRange(DateOnly fromDay, DateOnly toDay) =>
        (StartOf(fromDay), StartOf(toDay));
}
