using ConCancha.Contracts.Subscriptions;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<ClubSubscriptionDto>> GetClubSubscriptionAsync(CancellationToken cancellationToken) =>
        SendAsync<ClubSubscriptionDto>(HttpMethod.Get, ApiRoutes.ClubSubscription, body: null, cancellationToken);
}
