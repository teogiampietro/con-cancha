namespace ConCancha.Domain.Bookings;

public enum BookingRule
{
    Cancelled,
    NotAReservation,
    DifferentDuration,
    SameSlot,
    NotPaid,
    NoAttendanceToUndo,
    InvalidPaymentMethod,
}
