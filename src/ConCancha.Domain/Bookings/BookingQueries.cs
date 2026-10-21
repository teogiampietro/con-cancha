namespace ConCancha.Domain.Bookings;

/// <summary>Query-side twin of <see cref="BookingAmounts.ToCollect"/>, so the database can filter by it.</summary>
public static class BookingQueries
{
    public static IQueryable<Booking> WhereToCollect(this IQueryable<Booking> bookings) =>
        bookings.Where(booking => booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
            && booking.Attendance != Attendance.NoShow && booking.PaymentStatus == PaymentStatus.Pending && booking.Price > 0m);
}
