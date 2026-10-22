namespace ConCancha.Application.Emails;

public static class EmailTemplates
{
    private const string PasswordResetSubject = "Restablecé tu contraseña de ConCancha";
    private const string PasswordResetBodyTemplate =
        "Para elegir una contraseña nueva abrí este link (vale por un rato):\n{0}\n\nSi no lo pediste vos, ignorá este mensaje.";

    public static EmailContent PasswordReset(string resetLink) =>
        new(PasswordResetSubject, string.Format(PasswordResetBodyTemplate, resetLink));
}
