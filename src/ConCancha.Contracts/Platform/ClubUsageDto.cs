using ConCancha.Contracts.Subscriptions;
using ConCancha.Domain.Platform;

namespace ConCancha.Contracts.Platform;

/// <summary>How much a club uses ConCancha. Turns are the counted ones, on the day they are played.</summary>
/// <param name="BookingsInPeriod">Turns on the days of the chosen period.</param>
/// <param name="BookingsInPreviousPeriod">Turns on the days of the period right before it.</param>
/// <param name="OnlineBookingsInPeriod">Turns in the period that came through the public site.</param>
/// <param name="AverageBookingsPerDay">Turns in the period divided by its days.</param>
/// <param name="LastBookingOn">Day the club last loaded or received a reservation; <c>null</c> when it never did.</param>
/// <param name="Subscription">Trial and payments, as of now.</param>
public record ClubUsageDto(
    Guid Id,
    string Name,
    string Slug,
    string? Phone,
    string? OwnerEmail,
    DateOnly CreatedOn,
    ClubActivity Activity,
    int ActiveCourts,
    int Customers,
    int BookingsInPeriod,
    int BookingsInPreviousPeriod,
    int OnlineBookingsInPeriod,
    double AverageBookingsPerDay,
    DateOnly? LastBookingOn,
    ClubSubscriptionDto Subscription);
