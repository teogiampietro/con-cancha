using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Bookings;

namespace ConCancha.Contracts.RecurringBookings;

public record RecurringBookingRequest(
    DayOfWeek Day,
    int StartMinuteOfDay,
    [Required(ErrorMessage = "Poné el nombre de quien juega."),
     MinLength(BookingLimits.CustomerNameMinLength, ErrorMessage = "Poné el nombre de quien juega."),
     MaxLength(BookingLimits.CustomerNameMaxLength)] string CustomerName,
    [MaxLength(BookingLimits.PhoneMaxLength)] string? CustomerPhone);
