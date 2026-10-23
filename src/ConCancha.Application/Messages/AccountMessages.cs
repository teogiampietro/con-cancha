namespace ConCancha.Application.Messages;

public static class AccountMessages
{
    public const string EmailAlreadyRegistered = "Ya hay una cuenta con ese mail. Entrá con tu contraseña.";
    public const string InvalidCredentials = "Mail o contraseña incorrectos.";
    public const string TooManyAttempts = "Demasiados intentos. Probá de nuevo en unos minutos.";
    public const string EmailNotVerified = "Falta confirmar tu mail. Te mandamos un código nuevo.";
    public const string InvalidVerificationCode = "El código no es correcto o ya venció. Pedí uno nuevo.";
    public const string CurrentPasswordIncorrect = "La contraseña actual no es correcta.";
}
