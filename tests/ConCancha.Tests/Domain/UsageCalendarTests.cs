using ConCancha.Domain.Platform;

namespace ConCancha.Tests.Domain;

public class UsageCalendarTests
{
    [Fact]
    public void WeekRunsFromMondayToSunday()
    {
        var sunday = new DateOnly(2026, 1, 11);
        var calendar = new UsageCalendar(sunday);

        Assert.Equal(DayOfWeek.Monday, calendar.WeekStart.DayOfWeek);
        Assert.Equal(sunday, calendar.WeekEnd);
        Assert.Equal(calendar.WeekStart.AddDays(7), calendar.NextWeekStart);
        Assert.Equal(calendar.WeekStart.AddDays(-7), calendar.LastWeekStart);
    }

    [Fact]
    public void StartOfWeekOfAMondayIsItself()
    {
        var monday = new DateOnly(2026, 1, 5);

        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
        Assert.Equal(monday, UsageCalendar.StartOfWeek(monday));
    }
}
