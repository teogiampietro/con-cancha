using System.Globalization;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Platform;

namespace ConCancha.Web.Services;

public static class PlatformFormat
{
    private const string Dash = "—";
    private const int Percent = 100;
    private const int Decimals = 1;
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-AR");

    public static string Date(DateOnly day) => day.ToString("dd/MM/yyyy", Spanish);

    public static string DateOrDash(DateOnly? day) => day is { } value ? Date(value) : Dash;

    public static string ShortDate(DateOnly day) => day.ToString("dd/MM", Spanish);

    public static string Range(DateOnly from, DateOnly to) => $"{Date(from)} al {Date(to)}";

    public static string Days(int days) => days == 1 ? "1 día" : $"{days} días";

    public static string Turns(int turns) => turns == 1 ? "1 turno" : $"{turns} turnos";

    public static string Average(double value) => value.ToString($"F{Decimals}", Spanish);

    /// <summary>Change against the previous period, in percent. A zero base has no percentage to show.</summary>
    public static string Change(int current, int previous)
    {
        if (previous == 0) return Dash;

        var change = (int)Math.Round((current - previous) * (double)Percent / previous);
        return change switch
        {
            0 => "Sin cambios",
            > 0 => $"+{change} %",
            _ => $"{change} %",
        };
    }

    public static int Share(int part, int whole) => whole == 0 ? 0 : part * Percent / whole;

    public static string Status(ClubActivity activity) => activity switch
    {
        ClubActivity.Active => "Activo",
        ClubActivity.New => "Nuevo",
        _ => "Inactivo",
    };

    public static string StatusTone(ClubActivity activity) => activity switch
    {
        ClubActivity.Active => "ok",
        ClubActivity.Inactive => "warn",
        _ => string.Empty,
    };

    public static string Sport(Sport sport) => sport switch
    {
        Domain.Courts.Sport.Padel => "Pádel",
        Domain.Courts.Sport.Football => "Fútbol 11",
        Domain.Courts.Sport.Tennis => "Tenis",
        Domain.Courts.Sport.Pickleball => "Pickleball",
        Domain.Courts.Sport.FiveASideFootball => "Fútbol 5",
        Domain.Courts.Sport.Volleyball => "Vóley",
        Domain.Courts.Sport.Basketball => "Básquet",
        _ => sport.ToString(),
    };
}
