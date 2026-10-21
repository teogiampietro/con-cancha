using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Web.Services;

/// <summary>Talks to the API. Never throws for business errors: returns an <see cref="ApiResult{T}"/> with the message to show.</summary>
public sealed class ApiClient(HttpClient http, CurrentSession session, ClientIpAccessor clientIp)
{
    private const string BearerScheme = "Bearer";
    private const string ForwardedForHeader = "X-Forwarded-For";
    private const string ConnectionErrorMessage = "No pudimos conectarnos. Probá de nuevo en un momento.";
    private const string TimeoutMessage = "La conexión tardó demasiado. Probá de nuevo.";
    private const string TooManyRequestsMessage = "Demasiados intentos. Esperá un minuto y probá de nuevo.";
    private const string UnknownErrorMessage = "Algo salió mal. Probá de nuevo.";
    private const int NoStatusCode = 0;

    public Task<ApiResult<SessionResponse>> RegisterAsync(string email, string password, CancellationToken cancellationToken) =>
        SendAsync<SessionResponse>(HttpMethod.Post, ApiRoutes.Register, new RegisterRequest(email, password), cancellationToken);

    public Task<ApiResult<SessionResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken) =>
        SendAsync<SessionResponse>(HttpMethod.Post, ApiRoutes.Login, new LoginRequest(email, password), cancellationToken);

    public Task<ApiResult<object>> RequestPasswordResetAsync(string email, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, ApiRoutes.ForgotPassword, new ForgotPasswordRequest(email), cancellationToken);

    public Task<ApiResult<object>> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, ApiRoutes.ResetPassword, new ResetPasswordRequest(email, token, newPassword), cancellationToken);

    public Task<ApiResult<SessionResponse>> CreateClubAsync(
        string name, string? slug, string? phone, CancellationToken cancellationToken) =>
        SendAsync<SessionResponse>(HttpMethod.Post, ApiRoutes.Clubs, new CreateClubRequest(name, slug, phone), cancellationToken);

    public Task<ApiResult<SlugAvailabilityResponse>> GetSlugAvailabilityAsync(string slug, CancellationToken cancellationToken) =>
        SendAsync<SlugAvailabilityResponse>(
            HttpMethod.Get, $"{ApiRoutes.SlugAvailability}?slug={Uri.EscapeDataString(slug)}", body: null, cancellationToken);

    public Task<ApiResult<ClubDto>> GetCurrentClubAsync(CancellationToken cancellationToken) =>
        SendAsync<ClubDto>(HttpMethod.Get, ApiRoutes.CurrentClub, body: null, cancellationToken);

    public Task<ApiResult<IReadOnlyList<ClubMemberDto>>> GetCurrentClubTeamAsync(CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<ClubMemberDto>>(HttpMethod.Get, ApiRoutes.CurrentClubTeam, body: null, cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string route, object? body, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(method, route, body);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? await ReadSuccessAsync<T>(response, cancellationToken)
                : await ReadErrorAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure(ConnectionErrorMessage, NoStatusCode);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure(TimeoutMessage, NoStatusCode);
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string route, object? body)
    {
        var request = new HttpRequestMessage(method, route);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (session.Current?.Token is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
        if (!string.IsNullOrEmpty(clientIp.Address))
            request.Headers.TryAddWithoutValidation(ForwardedForHeader, clientIp.Address);
        return request;
    }

    private static async Task<ApiResult<T>> ReadSuccessAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var hasBody = typeof(T) != typeof(object) && response.Content.Headers.ContentLength != 0;
        var value = hasBody ? await response.Content.ReadFromJsonAsync<T>(cancellationToken) : default;
        return ApiResult<T>.Success(value, (int)response.StatusCode);
    }

    private static async Task<ApiResult<T>> ReadErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        if (statusCode == StatusCodes.Status429TooManyRequests)
            return ApiResult<T>.Failure(TooManyRequestsMessage, statusCode);

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return ParseProblemDetails<T>(document.RootElement, statusCode);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Failure(UnknownErrorMessage, statusCode);
        }
    }

    private static ApiResult<T> ParseProblemDetails<T>(JsonElement root, int statusCode)
    {
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            return ApiResult<T>.Invalid(ParseFieldErrors(errors), statusCode);

        if (root.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } message)
            return ApiResult<T>.Failure(message, statusCode);

        return ApiResult<T>.Failure(UnknownErrorMessage, statusCode);
    }

    private static Dictionary<string, string[]> ParseFieldErrors(JsonElement errors) =>
        errors.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.EnumerateArray().Select(message => message.GetString() ?? string.Empty).ToArray());
}
