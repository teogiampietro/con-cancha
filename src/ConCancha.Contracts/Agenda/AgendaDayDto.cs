namespace ConCancha.Contracts.Agenda;

public record AgendaDayDto(DateOnly Date, IReadOnlyList<AgendaCourtDto> Courts);
