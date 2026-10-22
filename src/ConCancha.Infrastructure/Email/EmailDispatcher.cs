using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Email;

/// <summary>Delivers queued messages off the request path, retrying transient provider failures with a growing delay.</summary>
public sealed class EmailDispatcher(
    EmailQueue queue,
    IEmailTransport transport,
    TimeProvider timeProvider,
    ILogger<EmailDispatcher> logger) : BackgroundService
{
    public static readonly IReadOnlyList<TimeSpan> RetryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            await DeliverWithRetriesAsync(email, stoppingToken).ConfigureAwait(false);
    }

    private async Task DeliverWithRetriesAsync(OutgoingEmail email, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (await TryDeliverAsync(email, attempt, cancellationToken).ConfigureAwait(false))
                return;

            if (attempt >= RetryDelays.Count)
            {
                logger.LogError("Email to {Recipient} dropped after {Attempts} attempts", Recipient(email), attempt + 1);
                return;
            }

            await Task.Delay(RetryDelays[attempt], timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <returns><c>true</c> when the message is done with: delivered or permanently rejected.</returns>
    private async Task<bool> TryDeliverAsync(OutgoingEmail email, int attempt, CancellationToken cancellationToken)
    {
        try
        {
            await transport.DeliverAsync(email, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Email \"{Subject}\" delivered to {Recipient}", email.Subject, Recipient(email));
            return true;
        }
        catch (EmailDeliveryException exception)
        {
            return LogFailure(email, attempt, exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // An unexpected bug must not stop the dispatcher, which would halt every later message.
            logger.LogError(exception, "Email to {Recipient} failed unexpectedly", Recipient(email));
            return true;
        }
    }

    private bool LogFailure(OutgoingEmail email, int attempt, EmailDeliveryException exception)
    {
        if (!exception.IsTransient)
        {
            logger.LogError(exception, "Email to {Recipient} rejected by the provider", Recipient(email));
            return true;
        }

        logger.LogWarning(exception, "Email to {Recipient} failed on attempt {Attempt}; will retry", Recipient(email), attempt + 1);
        return false;
    }

    private static string Recipient(OutgoingEmail email) => EmailRecipientMask.Mask(email.To);
}
