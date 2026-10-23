namespace ConCancha.Contracts.RecurringBookings;

public record RecurringBookingDto(
    Guid Id,
    Guid CourtId,
    DayOfWeek Day,
    int StartMinuteOfDay,
    string CustomerName,
    string? CustomerPhone,
    bool IsActive);
