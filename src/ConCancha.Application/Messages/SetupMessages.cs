namespace ConCancha.Application.Messages;

public static class SetupMessages
{
    public const string AlreadyCompleted = "La configuración inicial ya está hecha.";
    public const string SportsRequired = "Elegí al menos un deporte.";
    public const string DuplicateSport = "Cada deporte se elige una sola vez.";
    public const string InvalidCourtCount = "Cargá entre 1 y 20 canchas por deporte.";
    public const string PriceRequired = "Poné el precio de cada deporte.";
    public const string NightPriceIncomplete = "Poné el precio y el horario de la noche, o dejá los dos vacíos.";
    public const string InvalidNightStart = "La hora de la noche tiene que estar dentro del horario del club.";
}
