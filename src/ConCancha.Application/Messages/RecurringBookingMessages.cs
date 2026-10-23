namespace ConCancha.Application.Messages;

public static class RecurringBookingMessages
{
    public const string NotFound = "No encontramos ese turno fijo.";
    public const string SlotAlreadyFixed = "Ese horario ya tiene un turno fijo.";
    public const string InvalidDay = "Elegí un día de la semana.";
    public const string PriceMissing = "Primero poné el precio de la cancha para ese día y horario.";
}
