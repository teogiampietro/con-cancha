using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;

namespace ConCancha.Domain.Bookings;

/// <summary>A reservation or a block of a court's time. The database forbids two non-cancelled bookings overlapping on a court.</summary>
public class Booking : IBelongsToClub
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }

    public Guid CourtId { get; set; }
    public Court? Court { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public BookingKind Kind { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public Guid? CustomerId { get; set; }
    public BookingOrigin Origin { get; set; } = BookingOrigin.Panel;

    /// <summary>SHA-256 of the secret in the player's management link. Never the secret itself.</summary>
    public string? ManagementTokenHash { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal Price { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod? PaymentMethod { get; set; }
    public Attendance Attendance { get; set; } = Attendance.Pending;

    /// <summary>Why the time is blocked (rain, maintenance…).</summary>
    public string? Reason { get; set; }

    /// <summary>When the club marked the WhatsApp reminder as sent.</summary>
    public DateTimeOffset? ReminderSentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }
}
