namespace ConCancha.Contracts.Clubs;

public record ClubDto(
    Guid Id,
    string Name,
    string Slug,
    string? Phone,
    string CountryCode,
    int OpeningMinuteOfDay,
    int ClosingMinuteOfDay);
