using ConCancha.Domain.Courts;

namespace ConCancha.Contracts.Platform;

public record CourtUsageDto(Guid Id, string Name, Sport Sport, bool IsActive, int BookingsInPeriod);
