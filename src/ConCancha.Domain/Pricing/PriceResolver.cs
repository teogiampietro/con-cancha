namespace ConCancha.Domain.Pricing;

/// <summary>
/// Picks the winning rule for a moment: one with specific days beats one for every day;
/// on a tie the narrower time window wins.
/// </summary>
public static class PriceResolver
{
    public static decimal? Resolve(IEnumerable<PriceRule> rules, DayOfWeek day, int minuteOfDay)
    {
        var weekday = WeekdaysExtensions.From(day);
        return rules
            .Where(rule => rule.AppliesTo(weekday, minuteOfDay))
            .OrderByDescending(rule => rule.Days is not null)
            .ThenBy(rule => rule.ToMinuteOfDay - rule.FromMinuteOfDay)
            .ThenBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id)
            .Select(rule => (decimal?)rule.Price)
            .FirstOrDefault();
    }
}
