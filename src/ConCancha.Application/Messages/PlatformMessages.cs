namespace ConCancha.Application.Messages;

public static class PlatformMessages
{
    public const string ClubNotFound = "No encontramos ese club.";
    public const string InvalidCredentials = "Usuario o contraseña incorrectos.";
    public const string InvalidPage = "La página tiene que ser 1 o más.";
    public const string PeriodIncomplete = "Indicá las dos fechas del período.";
    public const string InvalidPeriod = "El período no es válido: desde no puede ser posterior a hasta, y no puede pasar de 366 días.";

    public static string TooManyAttempts(int minutes) => $"Demasiados intentos. Probá de nuevo en {minutes} minutos.";
}
