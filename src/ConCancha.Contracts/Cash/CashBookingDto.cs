using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.Cash;

public record CashBookingDto(
    int StartMinuteOfDay,
    string CustomerName,
    string CourtName,
    decimal Price,
    PaymentStatus PaymentStatus,
    PaymentMethod? PaymentMethod,
    Attendance Attendance);
