using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Domain.Bookings;

namespace ConCancha.Infrastructure.Services;

public static class BookingRuleErrors
{
    private const string PaymentMethodField = "paymentMethod";
    private const string StartField = "startMinuteOfDay";

    public static ServiceError ToServiceError(BookingRule rule) => rule switch
    {
        BookingRule.InvalidPaymentMethod => ServiceError.Validation(PaymentMethodField, BookingMessages.PaymentMethodRequired),
        BookingRule.DifferentDuration => ServiceError.Validation(StartField, BookingMessages.DifferentDuration),
        _ => ServiceError.Conflict(MessageFor(rule)),
    };

    private static string MessageFor(BookingRule rule) => rule switch
    {
        BookingRule.Cancelled => BookingMessages.CancelledCannotChange,
        BookingRule.NotAReservation => BookingMessages.NotAReservation,
        BookingRule.SameSlot => BookingMessages.SameSlot,
        BookingRule.NotPaid => BookingMessages.NotPaid,
        BookingRule.NoAttendanceToUndo => BookingMessages.NoAttendanceToUndo,
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, null),
    };

}
