using ConCancha.Domain.Common;

namespace ConCancha.Domain.Courts;

public readonly record struct OperatingHours(int OpeningMinuteOfDay, int ClosingMinuteOfDay)
{
    public static OperatingHours For(Court court, int clubOpening, int clubClosing) =>
        court.OpeningMinuteOfDay is { } opening && court.ClosingMinuteOfDay is { } closing
            ? new OperatingHours(opening, closing)
            : new OperatingHours(clubOpening, clubClosing);

    public static string? Validate(int opening, int closing)
    {
        if (opening < 0 || closing > TimeConstants.MinutesPerDay) return "El horario tiene que estar dentro del día.";
        if (opening >= closing) return "La apertura tiene que ser antes del cierre.";
        return null;
    }
}
