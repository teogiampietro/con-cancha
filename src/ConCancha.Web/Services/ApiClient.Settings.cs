using System.Net.Http.Headers;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    private const string LogoFormField = "file";

    public Task<ApiResult<ClubDto>> UpdateClubProfileAsync(UpdateClubProfileRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubProfile, request, cancellationToken);

    public Task<ApiResult<ClubDto>> UpdateClubCancellationAsync(UpdateCancellationRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubCancellation, request, cancellationToken);

    public Task<ApiResult<ClubDto>> UpdateClubColorsAsync(UpdateClubColorsRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubColors, request, cancellationToken);

    public Task<ApiResult<ClubDto>> UploadClubLogoAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { file, LogoFormField, fileName } };
        return SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubLogo, form, cancellationToken);
    }

    public Task<ApiResult<ClubDto>> RemoveClubLogoAsync(CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Delete, ApiRoutes.ClubLogo, body: null, cancellationToken);

    public Task<ApiResult<ClubDto>> UpdateClubModulesAsync(UpdateClubModulesRequest request, CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Put, ApiRoutes.ClubModules, request, cancellationToken);

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, ApiRoutes.ChangePassword, request, cancellationToken);
}
