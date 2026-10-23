using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.Bookings;

public record BookingDto(
    Guid Id,
    Guid CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateOnly Date,
    int StartMinuteOfDay,
    int EndMinuteOfDay,
    BookingKind Kind,
    BookingStatus Status,
    string? CustomerName,
    string? CustomerPhone,
    decimal Price,
    PaymentStatus PaymentStatus,
    PaymentMethod? PaymentMethod,
    Attendance Attendance,
    string? Reason,
    BookingOrigin Origin,
    Guid? RecurringBookingId = null);
