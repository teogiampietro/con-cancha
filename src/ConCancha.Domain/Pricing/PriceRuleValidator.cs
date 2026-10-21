using ConCancha.Domain.Common;

namespace ConCancha.Domain.Pricing;

public static class PriceRuleValidator
{
    public static string? Validate(Weekdays? days, int fromMinuteOfDay, int toMinuteOfDay, decimal price)
    {
        if (days is { } selected && (selected == Weekdays.None || (selected & ~Weekdays.All) != 0))
            return "Elegí al menos un día de la semana.";
        if (fromMinuteOfDay < 0 || toMinuteOfDay > TimeConstants.MinutesPerDay) return "El horario tiene que estar dentro del día.";
        if (fromMinuteOfDay >= toMinuteOfDay) return "El horario \"desde\" tiene que ser antes que \"hasta\".";
        return ValidatePrice(price);
    }

    public static string? ValidatePrice(decimal price) =>
        price is < 0 or > PriceLimits.MaxPrice ? "Poné un precio válido." : null;
}
