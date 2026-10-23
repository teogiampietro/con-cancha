namespace ConCancha.Contracts.Platform;

public record PlatformTotalsDto(
    int Clubs,
    int ActiveClubs,
    int ActiveCourts,
    int Customers,
    int BookingsInPeriod,
    int BookingsInPreviousPeriod,
    int OnlineBookingsInPeriod);
