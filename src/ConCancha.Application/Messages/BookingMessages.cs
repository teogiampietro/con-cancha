namespace ConCancha.Application.Messages;

public static class BookingMessages
{
    public const string BookingNotFound = "No encontramos ese turno.";
    public const string SlotTaken = "Ese horario ya está ocupado.";
    public const string NotASlot = "Ese horario no existe en la cancha.";
    public const string PriceRequired = "Poné el precio del turno.";
    public const string AlreadyCancelled = "El turno ya está cancelado.";
    public const string CancelledCannotChange = "El turno está cancelado.";
    public const string BlockCannotBePaid = "Un bloqueo no se cobra.";
    public const string PaymentMethodRequired = "Elegí cómo pagó.";
    public const string AlreadyPaid = "El turno ya está pagado.";
    public const string PaidCannotBeNoShow = "Un turno pagado no puede marcarse como \"no vino\".";
    public const string NotAReservation = "Esta acción es solo para reservas.";
    public const string TryAgain = "No pudimos guardar el turno. Probá de nuevo.";
    public const string CourtInactive = "La cancha está desactivada.";
}
