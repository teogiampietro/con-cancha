using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;

namespace ConCancha.Infrastructure.Services.Mapping;

public static class CourtMapper
{
    public static CourtDto ToDto(Court court) => new(
        court.Id, court.Name, court.Sport, court.SlotMinutes,
        court.OpeningMinuteOfDay, court.ClosingMinuteOfDay, court.IsActive);

    public static PriceRuleDto ToDto(PriceRule rule) => new(
        rule.Id, rule.CourtId, rule.Days, rule.FromMinuteOfDay, rule.ToMinuteOfDay, rule.Price);
}
