namespace ConCancha.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary><see cref="EmailProvider.Log"/> only writes to the log; production must use a real provider.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Resend;

    /// <summary>Sender with a domain verified at the provider, e.g. <c>ConCancha &lt;no-responder@dominio&gt;</c>.</summary>
    public string From { get; set; } = string.Empty;

    public string ResendApiKey { get; set; } = string.Empty;

    public bool IsComplete =>
        Provider == EmailProvider.Log || (!string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(ResendApiKey));
}
