namespace ConCancha.Contracts.Platform;

/// <param name="Start">The day, or the Monday that starts the week.</param>
public record BookingCountDto(DateOnly Start, int Bookings);
