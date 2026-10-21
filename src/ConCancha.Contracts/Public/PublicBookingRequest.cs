using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;

namespace ConCancha.Contracts.Public;

public record PublicBookingRequest(
    Guid CourtId,
    DateOnly Date,
    int StartMinuteOfDay,
    [Required(ErrorMessage = "Poné tu nombre."),
     MinLength(BookingLimits.CustomerNameMinLength, ErrorMessage = "Poné tu nombre."),
     MaxLength(BookingLimits.CustomerNameMaxLength)] string Name,
    [Required(ErrorMessage = "Poné tu WhatsApp."), MaxLength(BookingLimits.PhoneMaxLength)] string Phone,
    [MaxLength(CustomerLimits.EmailMaxLength), EmailAddress(ErrorMessage = "Revisá tu mail.")] string? Email);
