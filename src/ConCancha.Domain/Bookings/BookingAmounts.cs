namespace ConCancha.Domain.Bookings;

/// <summary>What a booking has brought in and what it still owes. Blocks, cancelled turns and no-shows owe nothing.</summary>
public static class BookingAmounts
{
    public static decimal Collected(Booking booking) =>
        booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed && booking.PaymentStatus == PaymentStatus.Paid
            ? booking.Price
            : 0m;

    public static decimal ToCollect(Booking booking) =>
        booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
            && booking.Attendance != Attendance.NoShow && booking.PaymentStatus == PaymentStatus.Pending
            ? booking.Price
            : 0m;
}
