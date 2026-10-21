using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<IReadOnlyList<CourtDto>>> GetCourtsAsync(CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<CourtDto>>(HttpMethod.Get, ApiRoutes.Courts, body: null, cancellationToken);

    public Task<ApiResult<CourtDto>> CreateCourtAsync(CourtRequest request, CancellationToken cancellationToken) =>
        SendAsync<CourtDto>(HttpMethod.Post, ApiRoutes.Courts, request, cancellationToken);

    public Task<ApiResult<CourtDto>> UpdateCourtAsync(Guid courtId, CourtRequest request, CancellationToken cancellationToken) =>
        SendAsync<CourtDto>(HttpMethod.Put, $"{ApiRoutes.Courts}/{courtId}", request, cancellationToken);

    public Task<ApiResult<IReadOnlyList<PriceRuleDto>>> GetPriceRulesAsync(Guid courtId, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<PriceRuleDto>>(HttpMethod.Get, $"{ApiRoutes.Courts}/{courtId}/price-rules", body: null, cancellationToken);

    public Task<ApiResult<PriceRuleDto>> CreatePriceRuleAsync(
        Guid courtId, PriceRuleRequest request, CancellationToken cancellationToken) =>
        SendAsync<PriceRuleDto>(HttpMethod.Post, $"{ApiRoutes.Courts}/{courtId}/price-rules", request, cancellationToken);

    public Task<ApiResult<PriceRuleDto>> UpdatePriceRuleAsync(
        Guid ruleId, PriceRuleRequest request, CancellationToken cancellationToken) =>
        SendAsync<PriceRuleDto>(HttpMethod.Put, $"{ApiRoutes.PriceRules}/{ruleId}", request, cancellationToken);

    public Task<ApiResult<object>> DeletePriceRuleAsync(Guid ruleId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, $"{ApiRoutes.PriceRules}/{ruleId}", body: null, cancellationToken);

    public Task<ApiResult<ClubDto>> UpdateClubHoursAsync(UpdateClubHoursRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubHours, request, cancellationToken);
}
