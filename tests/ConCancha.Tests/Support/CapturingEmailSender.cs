using System.Collections.Concurrent;
using ConCancha.Application.Abstractions;

namespace ConCancha.Tests.Support;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        _sent.Enqueue(new SentEmail(to, subject, body));
        return Task.CompletedTask;
    }
}
