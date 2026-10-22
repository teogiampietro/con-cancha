namespace ConCancha.Domain.Bookings;

public static class BookingPolicy
{
    public const int MaxActiveBookingsPerPhone = 2;
    public const int DefaultCancellationWindowHours = 12;
    public const int MinCancellationWindowHours = 0;
    public const int MaxCancellationWindowHours = 24 * 7;

    /// <summary>The public site offers today and the six days after it.</summary>
    public const int PublicBookingDays = 7;

    public static string? ValidateCancellationWindow(int windowHours) =>
        windowHours is < MinCancellationWindowHours or > MaxCancellationWindowHours
            ? $"La ventana de cancelación va de {MinCancellationWindowHours} a {MaxCancellationWindowHours} horas."
            : null;

    public static bool CanCancelOnline(DateTimeOffset startsAt, DateTimeOffset now, int windowHours) =>
        startsAt - now >= TimeSpan.FromHours(windowHours);
}
