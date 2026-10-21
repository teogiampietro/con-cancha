namespace ConCancha.Contracts.Public;

public record PublicDayDto(DateOnly Date, IReadOnlyList<PublicCourtDto> Courts);
