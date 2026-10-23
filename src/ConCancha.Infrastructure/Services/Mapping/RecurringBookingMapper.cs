using ConCancha.Contracts.RecurringBookings;
using ConCancha.Domain.Bookings;

namespace ConCancha.Infrastructure.Services.Mapping;

public static class RecurringBookingMapper
{
    public static RecurringBookingDto ToDto(RecurringBooking recurring) => new(
        recurring.Id, recurring.CourtId, recurring.Day, recurring.StartMinuteOfDay,
        recurring.CustomerName, recurring.CustomerPhone, recurring.IsActive);
}
