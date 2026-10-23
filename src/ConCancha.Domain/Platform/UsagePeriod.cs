using ConCancha.Domain.Common;

namespace ConCancha.Domain.Platform;

/// <summary>
/// Days picked in the platform panel, both ends included. Its previous period is the range of the same length right
/// before it, so the two can be compared.
/// </summary>
public sealed record UsagePeriod
{
    public const int MaxDays = 366;
    private const int MaxDailyBars = 31;
    private const int AverageDecimals = 1;

    private UsagePeriod(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public DateOnly ToExclusive => To.AddDays(1);

    public int Days => To.DayNumber - From.DayNumber + 1;

    public UsagePeriod Previous => new(From.AddDays(-Days), From.AddDays(-1));

    public UsageGranularity Granularity => Days <= MaxDailyBars ? UsageGranularity.Day : UsageGranularity.Week;

    /// <returns>The period, or <c>null</c> when it ends before it starts or is longer than <see cref="MaxDays"/>.</returns>
    public static UsagePeriod? Create(DateOnly from, DateOnly to) =>
        to < from || to.DayNumber - from.DayNumber >= MaxDays ? null : new UsagePeriod(from, to);

    /// <summary>The day a turn is counted under in the chart: the day itself, or the Monday of its week.</summary>
    public DateOnly BucketOf(DateOnly day) => Granularity == UsageGranularity.Day ? day : UsageCalendar.StartOfWeek(day);

    public IReadOnlyList<DateOnly> Buckets()
    {
        var first = BucketOf(From);
        var step = Granularity == UsageGranularity.Day ? 1 : TimeConstants.DaysPerWeek;
        var count = (BucketOf(To).DayNumber - first.DayNumber) / step + 1;
        return Enumerable.Range(0, count).Select(index => first.AddDays(index * step)).ToList();
    }

    public double AveragePerDay(int bookings) => Math.Round((double)bookings / Days, AverageDecimals);
}
