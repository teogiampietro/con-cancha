namespace ConCancha.Application.Messages;

public static class AccountMessages
{
    public const string EmailAlreadyRegistered = "Ya hay una cuenta con ese mail. Entrá con tu contraseña.";
    public const string InvalidCredentials = "Mail o contraseña incorrectos.";
    public const string TooManyAttempts = "Demasiados intentos. Probá de nuevo en unos minutos.";
    public const string CurrentPasswordIncorrect = "La contraseña actual no es correcta.";
    public const string PasswordResetSubject = "Restablecé tu contraseña de ConCancha";
    public const string PasswordResetBodyTemplate =
        "Para elegir una contraseña nueva abrí este link (vale por un rato):\n{0}\n\nSi no lo pediste vos, ignorá este mensaje.";
}
