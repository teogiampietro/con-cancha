using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Subscriptions;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Subscriptions;

namespace ConCancha.Infrastructure.Services;

public sealed class ClubSubscriptionService(CurrentClubProvider currentClub, TimeProvider clock) : IClubSubscriptionService
{
    public async Task<Result<ClubSubscriptionDto>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        return club is null
            ? Result<ClubSubscriptionDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound))
            : Result<ClubSubscriptionDto>.Success(ToDto(club));
    }

    public async Task<bool> HasAccessAsync(CancellationToken cancellationToken)
    {
        var club = await currentClub.GetAsync(cancellationToken).ConfigureAwait(false);
        return club is null || ToDto(club).HasAccess;
    }

    private ClubSubscriptionDto ToDto(Club club) => SubscriptionMapper.ToDto(club.TrialEndsAt, club.PaidUntil, clock.GetUtcNow());
}
