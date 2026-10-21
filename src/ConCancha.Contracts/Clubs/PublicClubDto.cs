namespace ConCancha.Contracts.Clubs;

public record PublicClubDto(string Name, string Slug, string? Phone, int OpeningMinuteOfDay, int ClosingMinuteOfDay);
