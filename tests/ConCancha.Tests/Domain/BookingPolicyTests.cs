using ConCancha.Domain.Bookings;

namespace ConCancha.Tests.Domain;

public class BookingPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(12 * 60, true)]
    [InlineData(12 * 60 - 1, false)]
    [InlineData(48 * 60, true)]
    [InlineData(0, false)]
    public void CanCancelOnline_DependsOnTimeLeftBeforeTheTurn(int minutesAhead, bool expected)
    {
        Assert.Equal(expected, BookingPolicy.CanCancelOnline(Now.AddMinutes(minutesAhead), Now, windowHours: 12));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, -1, false)]
    [InlineData(24, 24 * 60, true)]
    [InlineData(24, 24 * 60 - 1, false)]
    public void CanCancelOnline_UsesTheWindowOfTheClub(int windowHours, int minutesAhead, bool expected)
    {
        Assert.Equal(expected, BookingPolicy.CanCancelOnline(Now.AddMinutes(minutesAhead), Now, windowHours));
    }
}
