using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<ClubDto>> UpdateClubProfileAsync(UpdateClubProfileRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubProfile, request, cancellationToken);

    public Task<ApiResult<ClubDto>> UpdateClubCancellationAsync(UpdateCancellationRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubCancellation, request, cancellationToken);

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, ApiRoutes.ChangePassword, request, cancellationToken);
}
