using System.Globalization;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Common;
using ConCancha.Domain.Platform;

namespace ConCancha.Web.Services;

public static class PlatformPeriods
{
    public const string DateFormat = "yyyy-MM-dd";
    private const int TrailingDays = 30;
    private const string DefaultMessage = "Elegí las dos fechas del período.";

    /// <summary>The day in Argentina, where every club is, so the panel's "today" is the same for everyone.</summary>
    public static DateOnly Today(TimeProvider clock) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(ClubDefaults.TimeZoneId)).DateTime);

    public static PlatformPeriod Resolve(PeriodShortcut shortcut, DateOnly today) => shortcut switch
    {
        PeriodShortcut.Today => new(today, today),
        PeriodShortcut.ThisWeek => Week(today, weeksBack: 0),
        PeriodShortcut.LastWeek => Week(today, weeksBack: 1),
        PeriodShortcut.ThisMonth => Month(today),
        PeriodShortcut.Last30Days => new(today.AddDays(1 - TrailingDays), today),
        _ => throw new ArgumentOutOfRangeException(nameof(shortcut), shortcut, null),
    };

    public static PeriodShortcut? ShortcutOf(PlatformPeriod period, DateOnly today) =>
        Enum.GetValues<PeriodShortcut>()
            .Cast<PeriodShortcut?>()
            .FirstOrDefault(shortcut => Resolve(shortcut!.Value, today) == period);

    /// <summary>The period in the URL, or this week when the URL does not hold a valid one.</summary>
    public static PlatformPeriod Parse(string? from, string? to, DateOnly today)
    {
        if (!TryParseDay(from, out var start) || !TryParseDay(to, out var end) || UsagePeriod.Create(start, end) is null)
            return Resolve(PeriodShortcut.ThisWeek, today);

        return new PlatformPeriod(start, end);
    }

    public static string Query(PlatformPeriod period) =>
        $"desde={Format(period.From)}&hasta={Format(period.To)}";

    public static string Format(DateOnly day) => day.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static bool TryParseDay(string? text, out DateOnly day) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

    public static string ValidationMessage(DateOnly from, DateOnly to) =>
        from > to ? "Desde no puede ser posterior a hasta." : $"El período no puede tener más de {UsagePeriod.MaxDays} días.";

    public static string MissingDatesMessage => DefaultMessage;

    private static PlatformPeriod Week(DateOnly today, int weeksBack)
    {
        var monday = UsageCalendar.StartOfWeek(today).AddDays(-weeksBack * TimeConstants.DaysPerWeek);
        return new(monday, monday.AddDays(TimeConstants.DaysPerWeek - 1));
    }

    private static PlatformPeriod Month(DateOnly today)
    {
        var first = new DateOnly(today.Year, today.Month, 1);
        return new(first, first.AddMonths(1).AddDays(-1));
    }
}
