using ConCancha.Domain.Common;

namespace ConCancha.Domain.Clubs;

public static class ClubDefaults
{
    public const string CountryCode = "AR";
    public const string TimeZoneId = "America/Argentina/Buenos_Aires";
    public const int OpeningMinuteOfDay = 8 * TimeConstants.MinutesPerHour;
    public const int ClosingMinuteOfDay = TimeConstants.MinutesPerDay;
}
