using ConCancha.Domain.Bookings;

namespace ConCancha.Tests.Domain;

public class RecurringScheduleTests
{
    private static readonly DateOnly Wednesday = new(2026, 10, 21);

    [Fact]
    public void OccurrencesFrom_ReturnsTwelveWeeklyDates()
    {
        var dates = RecurringSchedule.OccurrencesFrom(Wednesday, DayOfWeek.Friday);

        Assert.Equal(RecurringSchedule.WindowWeeks, dates.Count);
        Assert.Equal(new DateOnly(2026, 10, 23), dates[0]);
        Assert.All(dates, date => Assert.Equal(DayOfWeek.Friday, date.DayOfWeek));
        Assert.All(dates.Zip(dates.Skip(1)), pair => Assert.Equal(RecurringSchedule.DaysPerWeek, pair.Second.DayNumber - pair.First.DayNumber));
    }

    [Fact]
    public void OccurrencesFrom_SameWeekdayAsToday_StartsToday()
    {
        var dates = RecurringSchedule.OccurrencesFrom(Wednesday, DayOfWeek.Wednesday);

        Assert.Equal(Wednesday, dates[0]);
        Assert.Equal(RecurringSchedule.WindowWeeks, dates.Count);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Tuesday)]
    public void OccurrencesFrom_NeverReachesBeyondTheWindow(DayOfWeek day)
    {
        var dates = RecurringSchedule.OccurrencesFrom(Wednesday, day);

        Assert.All(dates, date => Assert.InRange(date.DayNumber - Wednesday.DayNumber, 0, RecurringSchedule.WindowDays - 1));
    }
}
