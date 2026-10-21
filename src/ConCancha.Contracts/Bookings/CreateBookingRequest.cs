using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.Bookings;

/// <param name="Date">Day in the club's time zone.</param>
/// <param name="StartMinuteOfDay">Slot start, in minutes since local midnight.</param>
/// <param name="Price">Leave empty to use the court's price for that slot.</param>
public record CreateBookingRequest(
    Guid CourtId,
    DateOnly Date,
    int StartMinuteOfDay,
    [Required(ErrorMessage = "Poné el nombre."),
     MinLength(BookingLimits.CustomerNameMinLength, ErrorMessage = "Poné el nombre."),
     MaxLength(BookingLimits.CustomerNameMaxLength)] string CustomerName,
    [MaxLength(BookingLimits.PhoneMaxLength)] string? CustomerPhone,
    decimal? Price,
    bool IsPaid,
    PaymentMethod? PaymentMethod);
