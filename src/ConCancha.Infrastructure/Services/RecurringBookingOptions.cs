namespace ConCancha.Infrastructure.Services;

public sealed class RecurringBookingOptions
{
    public const string SectionName = "RecurringBookings";

    public bool RenewalEnabled { get; set; } = true;
    public int RenewalIntervalHours { get; set; } = 6;
}
