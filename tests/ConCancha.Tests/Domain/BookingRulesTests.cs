using ConCancha.Domain.Bookings;

namespace ConCancha.Tests.Domain;

public class BookingRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Length = TimeSpan.FromMinutes(90);

    private static Booking NewReservation(Action<Booking>? configure = null)
    {
        var booking = new Booking { CourtId = Guid.NewGuid(), StartsAt = Start, EndsAt = Start + Length, Kind = BookingKind.Reservation };
        configure?.Invoke(booking);
        return booking;
    }

    private static BookingRule RuleBrokenBy(Action action) => Assert.Throws<BookingRuleException>(action).Rule;

    [Fact]
    public void Move_ChangesCourtAndTime_AndResetsTheReminder()
    {
        var booking = NewReservation(b => b.ReminderSentAt = Start);
        var court = Guid.NewGuid();

        booking.Move(court, Start.AddHours(3), Start.AddHours(3) + Length);

        Assert.Equal(court, booking.CourtId);
        Assert.Equal(Start.AddHours(3), booking.StartsAt);
        Assert.Equal(Start.AddHours(3) + Length, booking.EndsAt);
        Assert.Null(booking.ReminderSentAt);
    }

    [Fact]
    public void Move_ToADifferentDuration_IsRejected()
    {
        var booking = NewReservation();

        Assert.Equal(BookingRule.DifferentDuration, RuleBrokenBy(() => booking.Move(Guid.NewGuid(), Start, Start.AddMinutes(60))));
    }

    [Fact]
    public void Move_ToTheSameSlot_IsRejected()
    {
        var booking = NewReservation();

        Assert.Equal(BookingRule.SameSlot, RuleBrokenBy(() => booking.Move(booking.CourtId, Start, Start + Length)));
    }

    [Fact]
    public void Move_CancelledOrBlock_IsRejected()
    {
        var cancelled = NewReservation(b => b.Status = BookingStatus.Cancelled);
        var block = NewReservation(b => b.Kind = BookingKind.Block);

        Assert.Equal(BookingRule.Cancelled, RuleBrokenBy(() => cancelled.Move(Guid.NewGuid(), Start, Start + Length)));
        Assert.Equal(BookingRule.NotAReservation, RuleBrokenBy(() => block.Move(Guid.NewGuid(), Start, Start + Length)));
    }

    [Theory]
    [InlineData(Attendance.Played)]
    [InlineData(Attendance.NoShow)]
    public void UndoAttendance_GoesBackToPending(Attendance attendance)
    {
        var booking = NewReservation(b => b.Attendance = attendance);

        booking.UndoAttendance();

        Assert.Equal(Attendance.Pending, booking.Attendance);
    }

    [Fact]
    public void UndoAttendance_WithNothingToUndo_IsRejected()
    {
        var booking = NewReservation();

        Assert.Equal(BookingRule.NoAttendanceToUndo, RuleBrokenBy(booking.UndoAttendance));
    }

    [Fact]
    public void MarkUnpaid_ClearsPaymentAndMethod()
    {
        var booking = NewReservation(b => (b.PaymentStatus, b.PaymentMethod) = (PaymentStatus.Paid, PaymentMethod.Cash));

        booking.MarkUnpaid();

        Assert.Equal(PaymentStatus.Pending, booking.PaymentStatus);
        Assert.Null(booking.PaymentMethod);
    }

    [Fact]
    public void MarkUnpaid_WhenNotPaid_IsRejected()
    {
        var booking = NewReservation();

        Assert.Equal(BookingRule.NotPaid, RuleBrokenBy(booking.MarkUnpaid));
    }

    [Fact]
    public void ChangePaymentMethod_OnlyWhenPaid_AndWithAKnownMethod()
    {
        var unpaid = NewReservation();
        var paid = NewReservation(b => (b.PaymentStatus, b.PaymentMethod) = (PaymentStatus.Paid, PaymentMethod.Cash));

        paid.ChangePaymentMethod(PaymentMethod.Transfer);

        Assert.Equal(PaymentMethod.Transfer, paid.PaymentMethod);
        Assert.Equal(BookingRule.NotPaid, RuleBrokenBy(() => unpaid.ChangePaymentMethod(PaymentMethod.Cash)));
        Assert.Equal(BookingRule.InvalidPaymentMethod, RuleBrokenBy(() => paid.ChangePaymentMethod((PaymentMethod)99)));
    }
}
