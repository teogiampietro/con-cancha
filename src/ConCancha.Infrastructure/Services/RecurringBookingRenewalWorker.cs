using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Services;

/// <summary>Runs the renewal at startup and then on a fixed interval, so the 12-week window never runs dry.</summary>
public sealed class RecurringBookingRenewalWorker(
    RecurringBookingRenewal renewal,
    IOptions<RecurringBookingOptions> options,
    TimeProvider timeProvider,
    ILogger<RecurringBookingRenewalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.RenewalEnabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.RenewalIntervalHours), timeProvider);
        do
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await renewal.RenewAllAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Fixed turns renewal failed; it will run again at the next interval");
        }
    }
}
