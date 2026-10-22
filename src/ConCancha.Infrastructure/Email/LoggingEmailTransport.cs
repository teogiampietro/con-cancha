using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Email;

/// <summary>
/// Local development stand-in: writes the whole message, link included, to the log so a reset can be finished
/// without a provider. Only configured for Development; the default provider is the real one.
/// </summary>
public sealed class LoggingEmailTransport(ILogger<LoggingEmailTransport> logger) : IEmailTransport
{
    public Task DeliverAsync(OutgoingEmail email, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Simulated email] To: {To} | Subject: {Subject}\n{Body}", email.To, email.Subject, email.Body);
        return Task.CompletedTask;
    }
}
