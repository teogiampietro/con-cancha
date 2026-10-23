using ConCancha.Application.Abstractions;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

/// <summary>Walks every club with active fixed turns and tops their window back up. A failing club never stops the others.</summary>
public sealed class RecurringBookingRenewal(IServiceScopeFactory scopeFactory, ILogger<RecurringBookingRenewal> logger)
{
    public async Task RenewAllAsync(CancellationToken cancellationToken)
    {
        foreach (var clubId in await FindClubIdsAsync(cancellationToken).ConfigureAwait(false))
            await RenewClubAsync(clubId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Guid>> FindClubIdsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        return await db.RecurringBookings.IgnoreQueryFilters().Where(recurring => recurring.IsActive)
            .Select(recurring => recurring.ClubId).Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RenewClubAsync(Guid clubId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetClub(clubId);
            var scheduler = scope.ServiceProvider.GetRequiredService<RecurringBookingScheduler>();
            await scheduler.RenewClubAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Fixed turns of club {ClubId} could not be renewed", clubId);
        }
    }
}
