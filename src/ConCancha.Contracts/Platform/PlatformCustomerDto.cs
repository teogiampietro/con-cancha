namespace ConCancha.Contracts.Platform;

/// <param name="Phone">Normalized WhatsApp number, as the club stored it.</param>
/// <param name="BookingsInPeriod">Counted turns of this customer on the days of the period.</param>
/// <param name="LastBookingOn">Day of the customer's latest counted turn, over all time; <c>null</c> when they have none.</param>
public record PlatformCustomerDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    int BookingsInPeriod,
    DateOnly? LastBookingOn,
    DateOnly CreatedOn);
