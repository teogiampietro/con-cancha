using System.Globalization;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Common;
using ConCancha.Domain.Pricing;

namespace ConCancha.Web.Services;

/// <summary>Spanish (Argentina) formatting done by hand so it does not depend on the server's ICU data.</summary>
public static class DisplayFormat
{
    private static readonly NumberFormatInfo MoneyFormat = new() { NumberGroupSeparator = ".", NumberDecimalSeparator = "," };
    private static readonly string[] DayNames = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];
    private static readonly string[] MonthNames =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public static readonly IReadOnlyList<(Weekdays Day, string Label)> WeekdayLabels =
    [
        (Weekdays.Monday, "Lun"), (Weekdays.Tuesday, "Mar"), (Weekdays.Wednesday, "Mié"), (Weekdays.Thursday, "Jue"),
        (Weekdays.Friday, "Vie"), (Weekdays.Saturday, "Sáb"), (Weekdays.Sunday, "Dom"),
    ];

    public static string Money(decimal amount) => "$\u00A0" + amount.ToString("N0", MoneyFormat);

    /// <summary>Drops the trailing zeros the API sends (24000.00) so a number input shows "24000", not "24000,00".</summary>
    public static decimal Normalize(decimal amount) => amount / 1.000000000000000000000000000000000m;

    public static decimal? Normalize(decimal? amount) => amount is { } value ? Normalize(value) : null;

    public static string Time(int minuteOfDay)
    {
        var hour = minuteOfDay / TimeConstants.MinutesPerHour % TimeConstants.HoursPerDay;
        return $"{hour:00}:{minuteOfDay % TimeConstants.MinutesPerHour:00}";
    }

    public static bool TryParseTimeInput(string? text, bool isEndOfDay, out int minuteOfDay)
    {
        minuteOfDay = 0;
        if (!TimeOnly.TryParse(text?.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) return false;

        minuteOfDay = time.Hour * TimeConstants.MinutesPerHour + time.Minute;
        if (isEndOfDay && minuteOfDay == 0) minuteOfDay = TimeConstants.MinutesPerDay;
        return true;
    }

    /// <summary>"Todo el día", "desde las 18:00" or "de 08:00 a 12:00": midnight as an end reads as "no end".</summary>
    public static string TimeRange(int fromMinuteOfDay, int toMinuteOfDay)
    {
        if (fromMinuteOfDay == 0 && toMinuteOfDay == TimeConstants.MinutesPerDay) return "todo el día";
        return toMinuteOfDay == TimeConstants.MinutesPerDay
            ? $"desde las {Time(fromMinuteOfDay)}"
            : $"de {Time(fromMinuteOfDay)} a {Time(toMinuteOfDay)}";
    }

    public static string LongDate(DateOnly date) => $"{DayNames[(int)date.DayOfWeek]} {date.Day} de {MonthNames[date.Month - 1]}";

    public static string ShortDate(DateOnly date) => $"{DayNames[(int)date.DayOfWeek][..3]} {date.Day}";

    public static string Days(Weekdays? days)
    {
        if (days is null || days == Weekdays.All) return "Todos los días";
        return string.Join(", ", WeekdayLabels.Where(entry => days.Value.HasFlag(entry.Day)).Select(entry => entry.Label));
    }

    public static string PaymentMethodLabel(PaymentMethod method) => method == PaymentMethod.Cash ? "Efectivo" : "Transferencia";
}
