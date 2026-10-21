namespace ConCancha.Application.Messages;

public static class PublicBookingMessages
{
    public const string InvalidPhone = "Revisá tu WhatsApp: tiene que ser un celular de Argentina, con código de área.";
    public const string DateOutOfRange = "Solo podés reservar de hoy a los próximos 6 días.";
    public const string SlotAlreadyPassed = "Ese horario ya pasó.";
    public const string SlotWithoutPrice = "Ese turno todavía no tiene precio. Llamá al club para reservarlo.";
    public const string TooManyActiveBookings = "Ya tenés {0} turnos por delante. Cuando juegues uno o cancelés otro, podés reservar de nuevo.";
    public const string BookingNotFound = "No encontramos ese turno. Revisá el link que te mandamos.";
    public const string AlreadyCancelled = "Ese turno ya está cancelado.";
    public const string CancellationWindowClosed = "Ya no podés cancelar desde acá: falta menos de {0} horas para el turno. Llamá al club.";
}
