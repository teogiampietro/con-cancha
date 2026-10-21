using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.Bookings;

public record CreateBlockRequest(
    Guid CourtId,
    DateOnly Date,
    int StartMinuteOfDay,
    [Required(ErrorMessage = "Poné el motivo."), MaxLength(BookingLimits.ReasonMaxLength)] string Reason);
