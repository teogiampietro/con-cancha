using ConCancha.Contracts.Bookings;

namespace ConCancha.Contracts.Agenda;

/// <param name="Price">Court price for this slot; null when no price rule covers it.</param>
/// <param name="Booking">Null when the slot is free.</param>
public record AgendaSlotDto(int StartMinuteOfDay, int EndMinuteOfDay, decimal? Price, bool IsPast, BookingDto? Booking);
