namespace ConCancha.Contracts.Bookings;

/// <param name="StartMinuteOfDay">New start, in minutes since local midnight, on the same day as the booking.</param>
public record MoveBookingRequest(Guid CourtId, int StartMinuteOfDay);
