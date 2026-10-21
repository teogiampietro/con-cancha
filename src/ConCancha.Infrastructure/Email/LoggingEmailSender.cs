using ConCancha.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Email;

/// <summary>
/// Stand-in until a transactional email provider is chosen (open decision in the plan): writes the message
/// to the log instead of sending it. Implement <see cref="IEmailSender"/> and register it in its place.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Simulated email] To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
