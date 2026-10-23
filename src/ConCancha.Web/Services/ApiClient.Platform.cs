using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Contracts.Subscriptions;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<SessionResponse>> LoginPlatformAsync(string user, string password, CancellationToken cancellationToken) =>
        SendAsync<SessionResponse>(
            HttpMethod.Post, ApiRoutes.PlatformLogin, new PlatformLoginRequest(user, password), cancellationToken,
            credential: ApiCredential.None);

    public Task<ApiResult<PlatformOverviewDto>> GetPlatformOverviewAsync(PlatformPeriod period, CancellationToken cancellationToken) =>
        SendAsync<PlatformOverviewDto>(
            HttpMethod.Get, $"{ApiRoutes.PlatformOverview}?{ApiPeriod(period)}", body: null, cancellationToken,
            credential: ApiCredential.Platform);

    public Task<ApiResult<ClubUsageDetailDto>> GetPlatformClubAsync(Guid clubId, PlatformPeriod period, CancellationToken cancellationToken) =>
        SendAsync<ClubUsageDetailDto>(
            HttpMethod.Get, $"{ApiRoutes.PlatformClubs}/{clubId}?{ApiPeriod(period)}", body: null, cancellationToken,
            credential: ApiCredential.Platform);

    public Task<ApiResult<PlatformCustomersPageDto>> GetPlatformCustomersAsync(
        Guid clubId, PlatformPeriod period, string search, int page, CancellationToken cancellationToken) =>
        SendAsync<PlatformCustomersPageDto>(
            HttpMethod.Get,
            $"{ApiRoutes.PlatformClubs}/{clubId}/customers?{ApiPeriod(period)}&search={Uri.EscapeDataString(search)}&page={page}",
            body: null, cancellationToken, credential: ApiCredential.Platform);

    public Task<ApiResult<object>> ExtendTrialAsync(Guid clubId, int days, CancellationToken cancellationToken) =>
        SendAsync<object>(
            HttpMethod.Post, $"{ApiRoutes.PlatformClubs}/{clubId}/trial/extension", new ExtendTrialRequest(days), cancellationToken,
            credential: ApiCredential.Platform);

    public Task<ApiResult<object>> MarkSubscriptionPaidAsync(Guid clubId, CancellationToken cancellationToken) =>
        SendAsync<object>(
            HttpMethod.Post, $"{ApiRoutes.PlatformClubs}/{clubId}/subscription/payment", body: null, cancellationToken,
            credential: ApiCredential.Platform);

    public Task<ApiResult<object>> SendRegularizationEmailAsync(Guid clubId, CancellationToken cancellationToken) =>
        SendAsync<object>(
            HttpMethod.Post, $"{ApiRoutes.PlatformClubs}/{clubId}/subscription/regularization-email", body: null, cancellationToken,
            credential: ApiCredential.Platform);

    /// <summary>The API's parameter names; the panel's own URLs use <c>desde</c> and <c>hasta</c>.</summary>
    private static string ApiPeriod(PlatformPeriod period) =>
        $"from={PlatformPeriods.Format(period.From)}&to={PlatformPeriods.Format(period.To)}";
}
