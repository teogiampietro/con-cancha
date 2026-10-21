using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;

namespace ConCancha.Infrastructure.Services;

public static class ClubMapper
{
    public static ClubDto ToDto(Club club) => new(
        club.Id, club.Name, club.Slug, club.Phone, club.CountryCode, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay);

    public static PublicClubDto ToPublicDto(Club club) => new(
        club.Name, club.Slug, club.Phone, club.OpeningMinuteOfDay, club.ClosingMinuteOfDay);
}
