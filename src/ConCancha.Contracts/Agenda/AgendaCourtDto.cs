using ConCancha.Contracts.Courts;

namespace ConCancha.Contracts.Agenda;

public record AgendaCourtDto(CourtDto Court, IReadOnlyList<AgendaSlotDto> Slots);
