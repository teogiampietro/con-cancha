using ConCancha.Domain.Pricing;

namespace ConCancha.Contracts.Pricing;

public record PriceRuleRequest(Weekdays? Days, int FromMinuteOfDay, int ToMinuteOfDay, decimal Price);
