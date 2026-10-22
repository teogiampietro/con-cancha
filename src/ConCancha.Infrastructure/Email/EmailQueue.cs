using System.Threading.Channels;

namespace ConCancha.Infrastructure.Email;

/// <summary>In-memory queue: messages still pending when the process stops are lost, which is acceptable for the pilot.</summary>
public sealed class EmailQueue
{
    private readonly Channel<OutgoingEmail> _channel =
        Channel.CreateUnbounded<OutgoingEmail>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(OutgoingEmail email)
    {
        if (!_channel.Writer.TryWrite(email))
            throw new InvalidOperationException("The email queue no longer accepts messages.");
    }

    public IAsyncEnumerable<OutgoingEmail> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
