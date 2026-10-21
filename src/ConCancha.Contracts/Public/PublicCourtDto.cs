using ConCancha.Domain.Courts;

namespace ConCancha.Contracts.Public;

public record PublicCourtDto(Guid Id, string Name, Sport Sport, int SlotMinutes, IReadOnlyList<PublicSlotDto> Slots);
