using ConCancha.Domain.Clubs;
using ConCancha.Domain.Common;

namespace ConCancha.Web.Services;

/// <summary>"Today" as the club lives it: the club's time zone, not the server's.</summary>
public sealed class ClubClock(ApiClient api, TimeProvider clock)
{
    private TimeZoneInfo? _zone;

    public async Task<DateOnly> GetTodayAsync(CancellationToken cancellationToken)
    {
        _zone ??= await LoadZoneAsync(cancellationToken);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _zone).DateTime);
    }

    public async Task<int> GetMinuteOfDayAsync(CancellationToken cancellationToken)
    {
        _zone ??= await LoadZoneAsync(cancellationToken);
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _zone);
        return local.Hour * TimeConstants.MinutesPerHour + local.Minute;
    }

    private async Task<TimeZoneInfo> LoadZoneAsync(CancellationToken cancellationToken)
    {
        var club = await api.GetCurrentClubAsync(cancellationToken);
        return TimeZoneInfo.FindSystemTimeZoneById(club.Value?.TimeZoneId ?? ClubDefaults.TimeZoneId);
    }
}
