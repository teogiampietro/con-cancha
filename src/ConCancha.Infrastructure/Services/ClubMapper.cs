using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;

namespace ConCancha.Infrastructure.Services;

public static class ClubMapper
{
    public static ClubDto ToDto(Club club) => new(
        club.Id, club.Name, club.Slug, club.Phone, club.CountryCode, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay, club.TimeZoneId,
        club.CancellationWindowHours, club.PrimaryColor, club.SecondaryColor, LogoVersion(club), club.EnabledModules);

    public static PublicClubDto ToPublicDto(Club club) => new(
        club.Name, club.Slug, club.Phone, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay, club.CancellationWindowHours, club.TimeZoneId,
        club.PrimaryColor, club.SecondaryColor, LogoVersion(club));

    private static string? LogoVersion(Club club) =>
        club.LogoPath is null ? null : Path.GetFileNameWithoutExtension(club.LogoPath);
}
