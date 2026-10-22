using ConCancha.Domain.Clubs;

namespace ConCancha.Contracts.Clubs;

/// <param name="LogoVersion">Changes with every new logo so browsers refetch it; <c>null</c> when the club has none.</param>
public record PublicClubDto(
    string Name,
    string Slug,
    string? Phone,
    int OpeningMinuteOfDay,
    int ClosingMinuteOfDay,
    int CancellationWindowHours,
    string TimeZoneId,
    string PrimaryColor = ClubBranding.DefaultPrimaryColor,
    string SecondaryColor = ClubBranding.DefaultSecondaryColor,
    string? LogoVersion = null);
