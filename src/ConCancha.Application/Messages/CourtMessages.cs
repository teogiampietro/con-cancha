namespace ConCancha.Application.Messages;

public static class CourtMessages
{
    public const string CourtNotFound = "No encontramos esa cancha.";
    public const string PriceRuleNotFound = "No encontramos ese precio.";
    public const string InvalidSlotMinutes = "La duración del turno tiene que ser de 60, 90 o 120 minutos.";
    public const string HoursPairRequired = "Poné la apertura y el cierre, o dejá los dos vacíos.";
    public const string DuplicateName = "Ya hay una cancha con ese nombre.";
}
