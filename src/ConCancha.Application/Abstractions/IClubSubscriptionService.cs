using ConCancha.Application.Results;
using ConCancha.Contracts.Subscriptions;

namespace ConCancha.Application.Abstractions;

public interface IClubSubscriptionService
{
    /// <summary>The trial and payments of the signed-in club.</summary>
    Task<Result<ClubSubscriptionDto>> GetCurrentAsync(CancellationToken cancellationToken);

    /// <summary>Whether the signed-in club can use the panel. A request without a club is not blocked here.</summary>
    Task<bool> HasAccessAsync(CancellationToken cancellationToken);
}
