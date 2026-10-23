using ConCancha.Domain.Common;

namespace ConCancha.Domain.Platform;

/// <summary>Where "today" falls for the platform figures. Weeks run Monday to Sunday.</summary>
public sealed record UsageCalendar(DateOnly Today)
{
    public DateOnly WeekStart => StartOfWeek(Today);

    public DateOnly WeekEnd => NextWeekStart.AddDays(-1);

    public DateOnly NextWeekStart => WeekStart.AddDays(TimeConstants.DaysPerWeek);

    public DateOnly LastWeekStart => WeekStart.AddDays(-TimeConstants.DaysPerWeek);

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + TimeConstants.DaysPerWeek) % TimeConstants.DaysPerWeek;
        return date.AddDays(-daysSinceMonday);
    }
}
