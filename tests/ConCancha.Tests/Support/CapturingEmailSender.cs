using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ConCancha.Application.Abstractions;

namespace ConCancha.Tests.Support;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class CapturingEmailSender : IEmailSender
{
    private static readonly Regex VerificationCode = new(@"es: (\d+)");
    private static readonly ConcurrentDictionary<string, string> LatestCodes = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentQueue<SentEmail> _sent = new();

    /// <summary>Latest verification code emailed to the address; shared so extension helpers can finish a sign-up.</summary>
    public static string LatestCodeFor(string email) => LatestCodes[email];

    public IReadOnlyCollection<SentEmail> Sent => _sent;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        _sent.Enqueue(new SentEmail(to, subject, body));
        if (VerificationCode.Match(body) is { Success: true } match) LatestCodes[to] = match.Groups[1].Value;
        return Task.CompletedTask;
    }
}
