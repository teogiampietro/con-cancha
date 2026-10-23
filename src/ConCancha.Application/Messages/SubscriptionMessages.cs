namespace ConCancha.Application.Messages;

public static class SubscriptionMessages
{
    public const string TrialExpired = "La prueba del club terminó y no tiene una suscripción paga.";
    public const string TrialNotStarted = "La prueba arranca cuando el club termina de configurar sus canchas.";
    public const string InvalidExtensionDays = "Indicá entre 1 y 365 días.";
    public const string NothingToRegularize = "Esta cuenta no tiene pagos pendientes.";
    public const string OwnerWithoutEmail = "El club no tiene un dueño con mail.";
}
