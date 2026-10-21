using ConCancha.Contracts.Setup;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<SetupProgressDto>> GetSetupProgressAsync(CancellationToken cancellationToken) =>
        SendAsync<SetupProgressDto>(HttpMethod.Get, ApiRoutes.Setup, body: null, cancellationToken);

    public Task<ApiResult<object>> SaveSetupProgressAsync(int step, SetupAnswersDto draft, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Put, ApiRoutes.Setup, new SaveSetupProgressRequest(step, draft), cancellationToken);

    public Task<ApiResult<object>> CompleteSetupAsync(SetupAnswersDto answers, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, ApiRoutes.SetupComplete, answers, cancellationToken);
}
