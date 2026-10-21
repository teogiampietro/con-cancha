using ConCancha.Domain.Bookings;

namespace ConCancha.Domain.Clubs;

public class Club
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CountryCode { get; set; } = ClubDefaults.CountryCode;
    public string TimeZoneId { get; set; } = ClubDefaults.TimeZoneId;

    /// <summary>Club-wide default hours; each court may override them.</summary>
    public int OpeningMinuteOfDay { get; set; } = ClubDefaults.OpeningMinuteOfDay;
    public int ClosingMinuteOfDay { get; set; } = ClubDefaults.ClosingMinuteOfDay;

    public int CancellationWindowHours { get; set; } = BookingPolicy.DefaultCancellationWindowHours;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
