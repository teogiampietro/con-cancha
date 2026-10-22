using ConCancha.Domain.Clubs;

namespace ConCancha.Contracts.Clubs;

public record ClubDto(
    Guid Id,
    string Name,
    string Slug,
    string? Phone,
    string CountryCode,
    int OpeningMinuteOfDay,
    int ClosingMinuteOfDay,
    string TimeZoneId,
    int CancellationWindowHours,
    string PrimaryColor = ClubBranding.DefaultPrimaryColor,
    string SecondaryColor = ClubBranding.DefaultSecondaryColor,
    string? LogoVersion = null,
    ClubModules EnabledModules = ClubModules.All);
