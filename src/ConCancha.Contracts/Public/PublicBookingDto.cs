using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.Public;

/// <param name="CancelableUntil">Last moment the player can still cancel online; null once the booking is cancelled.</param>
public record PublicBookingDto(
    string ClubName,
    string? ClubPhone,
    string CourtName,
    DateOnly Date,
    int StartMinuteOfDay,
    int EndMinuteOfDay,
    decimal Price,
    BookingStatus Status,
    bool CanCancel,
    DateTimeOffset? CancelableUntil,
    int CancellationWindowHours);
