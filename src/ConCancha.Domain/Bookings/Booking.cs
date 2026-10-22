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

    /// <summary>SHA-256 of the secret a browser keeps to list its own bookings. Never the secret itself.</summary>
    public string? VisitorTokenHash { get; set; }

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

    /// <summary>Same day is the caller's rule (it knows the club's time zone); the duration cannot change.</summary>
    public void Move(Guid courtId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        RequireActiveReservation();
        if (endsAt - startsAt != EndsAt - StartsAt) throw new BookingRuleException(BookingRule.DifferentDuration);
        if (courtId == CourtId && startsAt == StartsAt) throw new BookingRuleException(BookingRule.SameSlot);

        CourtId = courtId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        ReminderSentAt = null;
    }

    public void UndoAttendance()
    {
        RequireActiveReservation();
        if (Attendance == Attendance.Pending) throw new BookingRuleException(BookingRule.NoAttendanceToUndo);

        Attendance = Attendance.Pending;
    }

    public void MarkUnpaid()
    {
        RequireActiveReservation();
        RequirePaid();

        PaymentStatus = PaymentStatus.Pending;
        PaymentMethod = null;
    }

    public void ChangePaymentMethod(PaymentMethod method)
    {
        RequireActiveReservation();
        RequirePaid();
        if (!Enum.IsDefined(method)) throw new BookingRuleException(BookingRule.InvalidPaymentMethod);

        PaymentMethod = method;
    }

    private void RequirePaid()
    {
        if (PaymentStatus != PaymentStatus.Paid) throw new BookingRuleException(BookingRule.NotPaid);
    }

    private void RequireActiveReservation()
    {
        if (Status == BookingStatus.Cancelled) throw new BookingRuleException(BookingRule.Cancelled);
        if (Kind != BookingKind.Reservation) throw new BookingRuleException(BookingRule.NotAReservation);
    }
}
