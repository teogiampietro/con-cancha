using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;

namespace ConCancha.Domain.Bookings;

/// <summary>A customer who plays the same court, weekday and hour every week. Its bookings are generated ahead of time and block the slot.</summary>
public class RecurringBooking : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public DayOfWeek Day { get; set; }
    public int StartMinuteOfDay { get; set; }

    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    /// <summary>False once a change to the court left this slot without a place in the grid; it stops generating bookings.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
