using ConCancha.Domain.Platform;

namespace ConCancha.Tests.Domain;

public class UsagePeriodTests
{
    private static readonly DateOnly January1 = new(2026, 1, 1);

    [Fact]
    public void Create_RejectsAPeriodThatEndsBeforeItStarts()
    {
        Assert.Null(UsagePeriod.Create(new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 9)));
    }

    [Fact]
    public void Create_AcceptsUpToMaxDaysAndRejectsOneMore()
    {
        Assert.NotNull(UsagePeriod.Create(January1, January1.AddDays(UsagePeriod.MaxDays - 1)));
        Assert.Null(UsagePeriod.Create(January1, January1.AddDays(UsagePeriod.MaxDays)));
    }

    [Fact]
    public void Days_CountBothEnds()
    {
        var period = UsagePeriod.Create(January1, January1.AddDays(6))!;

        Assert.Equal(7, period.Days);
        Assert.Equal(January1.AddDays(7), period.ToExclusive);
    }

    [Fact]
    public void Previous_IsTheSameLengthRightBeforeTheStart()
    {
        var period = UsagePeriod.Create(January1, January1.AddDays(6))!;

        Assert.Equal(new DateOnly(2025, 12, 25), period.Previous.From);
        Assert.Equal(new DateOnly(2025, 12, 31), period.Previous.To);
        Assert.Equal(period.Days, period.Previous.Days);
    }

    [Fact]
    public void Granularity_IsDailyUpToAMonthAndWeeklyAfterwards()
    {
        Assert.Equal(UsageGranularity.Day, UsagePeriod.Create(January1, January1.AddDays(30))!.Granularity);
        Assert.Equal(UsageGranularity.Week, UsagePeriod.Create(January1, January1.AddDays(31))!.Granularity);
    }

    [Fact]
    public void Buckets_OfADailyPeriodAreEachDay()
    {
        var period = UsagePeriod.Create(January1, January1.AddDays(6))!;

        var buckets = period.Buckets();

        Assert.Equal(7, buckets.Count);
        Assert.Equal(January1, buckets[0]);
        Assert.Equal(January1.AddDays(6), buckets[^1]);
    }

    [Fact]
    public void Buckets_OfAWeeklyPeriodAreEachMondayCoveringIt()
    {
        var from = new DateOnly(2026, 10, 21);
        var to = new DateOnly(2026, 12, 2);
        var period = UsagePeriod.Create(from, to)!;

        var expected = Enumerable.Range(0, 100)
            .Select(index => UsageCalendar.StartOfWeek(from).AddDays(index * 7))
            .TakeWhile(monday => monday <= to)
            .ToList();

        Assert.Equal(UsageGranularity.Week, period.Granularity);
        Assert.Equal(expected, period.Buckets());
        Assert.All(period.Buckets(), monday => Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek));
    }

    [Fact]
    public void BucketOf_MapsADayToItsMondayInAWeeklyPeriod()
    {
        var period = UsagePeriod.Create(January1, January1.AddDays(60))!;
        var sunday = new DateOnly(2026, 1, 11);

        Assert.Equal(DayOfWeek.Sunday, sunday.DayOfWeek);
        Assert.Equal(sunday.AddDays(-6), period.BucketOf(sunday));
    }

    [Theory]
    [InlineData(1, 3, 0.3)]
    [InlineData(9, 3, 3.0)]
    [InlineData(0, 7, 0.0)]
    public void AveragePerDay_DividesByTheDaysAndRoundsToOneDecimal(int bookings, int days, double expected)
    {
        var period = UsagePeriod.Create(January1, January1.AddDays(days - 1))!;

        Assert.Equal(expected, period.AveragePerDay(bookings));
    }
}
