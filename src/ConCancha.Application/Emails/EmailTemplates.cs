namespace ConCancha.Application.Emails;

public static class EmailTemplates
{
    private const string PasswordResetSubject = "Restablecé tu contraseña de ConCancha";
    private const string PasswordResetBodyTemplate =
        "Para elegir una contraseña nueva abrí este link (vale por un rato):\n{0}\n\nSi no lo pediste vos, ignorá este mensaje.";

    private const string EmailVerificationSubject = "Tu código de ConCancha";
    private const string EmailVerificationBodyTemplate =
        "Tu código para confirmar el mail es: {0}\n\nVale por unos minutos. Si no creaste una cuenta en ConCancha, ignorá este mensaje.";

    private const string TeamInvitationSubjectTemplate = "Te invitaron a sumarte a {0} en ConCancha";
    private const string TeamInvitationBodyTemplate =
        "Te invitaron a ayudar en el mostrador de {0}. Para crear tu contraseña y entrar abrí este link (vale por {1} días):\n{2}\n\nSi no esperabas esta invitación, ignorá este mensaje.";

    private const string SubscriptionPendingSubjectTemplate = "Pago pendiente de tu suscripción a ConCancha";
    private const string SubscriptionPendingBodyTemplate =
        "Hola. La suscripción de {0} a ConCancha está pendiente de pago. Para regularizarla, escribinos y te pasamos los datos para pagar.\n\nSi ya pagaste, escribinos para revisarlo.";

    private const string TeamMemberAddedSubjectTemplate = "Ya sos parte del equipo de {0} en ConCancha";
    private const string TeamMemberAddedBodyTemplate =
        "{0} te sumó como encargado. Entrá con tu contraseña de siempre:\n{1}";

    public static EmailContent PasswordReset(string resetLink) =>
        new(PasswordResetSubject, string.Format(PasswordResetBodyTemplate, resetLink));

    public static EmailContent EmailVerification(string code) =>
        new(EmailVerificationSubject, string.Format(EmailVerificationBodyTemplate, code));

    public static EmailContent TeamInvitation(string clubName, string acceptLink, int validDays) =>
        new(string.Format(TeamInvitationSubjectTemplate, clubName),
            string.Format(TeamInvitationBodyTemplate, clubName, validDays, acceptLink));

    public static EmailContent SubscriptionPending(string clubName) =>
        new(SubscriptionPendingSubjectTemplate, string.Format(SubscriptionPendingBodyTemplate, clubName));

    public static EmailContent TeamMemberAdded(string clubName, string panelLink) =>
        new(string.Format(TeamMemberAddedSubjectTemplate, clubName),
            string.Format(TeamMemberAddedBodyTemplate, clubName, panelLink));
}
