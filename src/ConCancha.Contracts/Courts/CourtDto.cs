using ConCancha.Domain.Courts;

namespace ConCancha.Contracts.Courts;

public record CourtDto(
    Guid Id,
    string Name,
    Sport Sport,
    int SlotMinutes,
    int? OpeningMinuteOfDay,
    int? ClosingMinuteOfDay,
    bool IsActive);
