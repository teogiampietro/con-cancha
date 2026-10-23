namespace ConCancha.Domain.Bookings;

public static class BookingQueries
{
    /// <summary>What the platform counts as a turn: a reservation still standing that the player did not miss.</summary>
    public static IQueryable<Booking> WhereCountedTurn(this IQueryable<Booking> bookings) =>
        bookings.Where(booking => booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
            && booking.Attendance != Attendance.NoShow);

    /// <summary>Query-side twin of <see cref="BookingAmounts.ToCollect"/>, so the database can filter by it.</summary>
    public static IQueryable<Booking> WhereToCollect(this IQueryable<Booking> bookings) =>
        bookings.Where(booking => booking.Kind == BookingKind.Reservation && booking.Status == BookingStatus.Confirmed
            && booking.Attendance != Attendance.NoShow && booking.PaymentStatus == PaymentStatus.Pending && booking.Price > 0m);
}
