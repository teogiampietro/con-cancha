using ConCancha.Domain.Clubs;
using ConCancha.Domain.Common;

namespace ConCancha.Infrastructure.Time;

/// <summary>Everything is stored in UTC; this converts to and from the club's local day only at the edges.</summary>
public sealed class ClubTimeZone(TimeZoneInfo zone)
{
    public static ClubTimeZone For(Club club) => new(TimeZoneInfo.FindSystemTimeZoneById(club.TimeZoneId));

    public DateTimeOffset ToUtc(DateOnly date, int minuteOfDay)
    {
        var local = date.ToDateTime(TimeOnly.MinValue).AddMinutes(minuteOfDay);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }

    public (DateOnly Date, int MinuteOfDay) ToLocal(DateTimeOffset moment)
    {
        var local = TimeZoneInfo.ConvertTime(moment, zone);
        return (DateOnly.FromDateTime(local.DateTime), local.Hour * TimeConstants.MinutesPerHour + local.Minute);
    }

    public (DateTimeOffset Start, DateTimeOffset End) GetDayRange(DateOnly date) =>
        (ToUtc(date, 0), ToUtc(date.AddDays(1), 0));
}
