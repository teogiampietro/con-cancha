using ConCancha.Contracts.Bookings;

namespace ConCancha.Contracts.Reservations;

public record ReservationItemDto(BookingDto Booking, string CourtName);
