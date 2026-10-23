using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;
using ConCancha.Infrastructure.Time;

namespace ConCancha.Infrastructure.Services.Mapping;

public static class BookingMapper
{
    public static BookingDto ToDto(Booking booking, ClubTimeZone zone)
    {
        var (date, startMinute) = zone.ToLocal(booking.StartsAt);
        var endMinute = startMinute + (int)(booking.EndsAt - booking.StartsAt).TotalMinutes;
        return new BookingDto(
            booking.Id, booking.CourtId, booking.StartsAt, booking.EndsAt, date, startMinute, endMinute,
            booking.Kind, booking.Status, booking.CustomerName, booking.CustomerPhone, booking.Price,
            booking.PaymentStatus, booking.PaymentMethod, booking.Attendance, booking.Reason, booking.Origin,
            booking.RecurringBookingId);
    }
}
