namespace ConCancha.Contracts.Cash;

/// <param name="Total">What came in: cash plus transfers.</param>
/// <param name="ToCollect">What the day's turns still owe.</param>
public record CashDayDto(
    DateOnly Date,
    decimal Cash,
    decimal Transfer,
    decimal Total,
    decimal ToCollect,
    IReadOnlyList<CashBookingDto> Bookings,
    CashClosureDto? Closure);
