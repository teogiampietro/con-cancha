namespace ConCancha.Domain.Bookings;

public static class RecurringSchedule
{
    public const int WindowWeeks = 12;
    public const int DaysPerWeek = 7;
    public const int WindowDays = WindowWeeks * DaysPerWeek;

    /// <summary>The dates, from <paramref name="today"/> on, on which a weekly turn falls inside the rolling window.</summary>
    public static IReadOnlyList<DateOnly> OccurrencesFrom(DateOnly today, DayOfWeek day)
    {
        var daysUntilFirst = ((int)day - (int)today.DayOfWeek + DaysPerWeek) % DaysPerWeek;
        var first = today.AddDays(daysUntilFirst);
        return Enumerable.Range(0, WindowWeeks).Select(week => first.AddDays(week * DaysPerWeek))
            .Where(date => date.DayNumber - today.DayNumber < WindowDays)
            .ToList();
    }
}
