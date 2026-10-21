using ConCancha.Domain.Bookings;

namespace ConCancha.Tests.Domain;

public class BookingAmountsTests
{
    private const decimal Price = 20000m;

    private static Booking Reservation(
        PaymentStatus payment = PaymentStatus.Pending,
        BookingStatus status = BookingStatus.Confirmed,
        Attendance attendance = Attendance.Pending,
        BookingKind kind = BookingKind.Reservation) =>
        new() { Price = Price, PaymentStatus = payment, Status = status, Attendance = attendance, Kind = kind };

    [Fact]
    public void PaidReservation_IsCollected_AndOwesNothing()
    {
        var booking = Reservation(PaymentStatus.Paid);

        Assert.Equal(Price, BookingAmounts.Collected(booking));
        Assert.Equal(0m, BookingAmounts.ToCollect(booking));
    }

    [Fact]
    public void UnpaidReservation_OwesItsPrice()
    {
        var booking = Reservation();

        Assert.Equal(0m, BookingAmounts.Collected(booking));
        Assert.Equal(Price, BookingAmounts.ToCollect(booking));
    }

    [Theory]
    [InlineData(BookingKind.Block, BookingStatus.Confirmed, Attendance.Pending)]
    [InlineData(BookingKind.Reservation, BookingStatus.Cancelled, Attendance.Pending)]
    [InlineData(BookingKind.Reservation, BookingStatus.Confirmed, Attendance.NoShow)]
    public void BlocksCancelledAndNoShows_OweNothing(BookingKind kind, BookingStatus status, Attendance attendance)
    {
        var booking = Reservation(status: status, attendance: attendance, kind: kind);

        Assert.Equal(0m, BookingAmounts.ToCollect(booking));
    }

    [Fact]
    public void CancelledPaidReservation_IsNotCollected()
    {
        Assert.Equal(0m, BookingAmounts.Collected(Reservation(PaymentStatus.Paid, BookingStatus.Cancelled)));
    }
}
