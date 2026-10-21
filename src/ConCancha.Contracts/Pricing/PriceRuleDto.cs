using ConCancha.Domain.Pricing;

namespace ConCancha.Contracts.Pricing;

public record PriceRuleDto(Guid Id, Guid CourtId, Weekdays? Days, int FromMinuteOfDay, int ToMinuteOfDay, decimal Price);
