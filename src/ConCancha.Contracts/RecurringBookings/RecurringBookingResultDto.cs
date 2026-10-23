namespace ConCancha.Contracts.RecurringBookings;

/// <param name="SkippedDates">Dates inside the window that stayed without a turn because the slot was taken or had no price.</param>
public record RecurringBookingResultDto(
    RecurringBookingDto Recurring,
    int CreatedCount,
    IReadOnlyList<DateOnly> SkippedDates);
