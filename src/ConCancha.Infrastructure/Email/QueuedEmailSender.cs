using ConCancha.Application.Abstractions;

namespace ConCancha.Infrastructure.Email;

public sealed class QueuedEmailSender(EmailQueue queue) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        queue.Enqueue(new OutgoingEmail(to, subject, body));
        return Task.CompletedTask;
    }
}
