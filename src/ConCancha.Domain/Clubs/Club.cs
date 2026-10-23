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

    /// <summary>Key of the logo in the file store; <c>null</c> when the club has none.</summary>
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = ClubBranding.DefaultPrimaryColor;
    public string SecondaryColor { get; set; } = ClubBranding.DefaultSecondaryColor;
    public ClubModules EnabledModules { get; set; } = ClubModules.All;

    /// <summary>When the free trial ends. Starts when the setup is completed, so it is <c>null</c> before that.</summary>
    public DateTimeOffset? TrialEndsAt { get; set; }

    /// <summary>Until when the current monthly payment covers the club; <c>null</c> for a club that never paid.</summary>
    public DateTimeOffset? PaidUntil { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
