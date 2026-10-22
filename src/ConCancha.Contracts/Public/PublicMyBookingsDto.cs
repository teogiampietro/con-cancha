namespace ConCancha.Contracts.Public;

/// <summary>Upcoming bookings made from the same browser at this club.</summary>
public record PublicMyBookingsDto(
    string ClubName,
    string? ClubPhone,
    int CancellationWindowHours,
    IReadOnlyList<PublicMyBookingDto> Bookings);
