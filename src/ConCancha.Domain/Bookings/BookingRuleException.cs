namespace ConCancha.Domain.Bookings;

public sealed class BookingRuleException(BookingRule rule) : Exception($"Booking rule violated: {rule}")
{
    public BookingRule Rule { get; } = rule;
}
