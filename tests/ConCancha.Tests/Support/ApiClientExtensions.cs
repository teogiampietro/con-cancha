using System.Net.Http.Headers;
using System.Net.Http.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;

namespace ConCancha.Tests.Support;

public static class ApiClientExtensions
{
    public const string ValidPassword = "una-clave-larga-123";
    private const string DefaultClubName = "Club de prueba";

    public static string NewEmail() => $"u{Guid.NewGuid():N}@prueba.test";

    public static string NewSlug() => "club-" + Guid.NewGuid().ToString("N")[..12];

    public static HttpClient WithBearer(this HttpClient http, string? token)
    {
        http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    public static async Task<SessionResponse> RegisterAsync(
        this HttpClient http, CancellationToken cancellationToken, string? email = null, string? password = null)
    {
        var request = new RegisterRequest(email ?? NewEmail(), password ?? ValidPassword);
        var registration = await http.PostAsJsonAsync(Routes.Register, request, cancellationToken);
        registration.EnsureSuccessStatusCode();

        var verification = new VerifyEmailRequest(request.Email, CapturingEmailSender.LatestCodeFor(request.Email));
        var response = await http.PostAsJsonAsync(Routes.VerifyEmail, verification, cancellationToken);
        return await ReadSessionAsync(response, cancellationToken);
    }

    public static async Task<HttpResponseMessage> LoginAsync(
        this HttpClient http, string email, string password, CancellationToken cancellationToken) =>
        await http.PostAsJsonAsync(Routes.Login, new LoginRequest(email, password), cancellationToken);

    public static async Task<HttpResponseMessage> CreateClubAsync(
        this HttpClient http, string token, CreateClubRequest request, CancellationToken cancellationToken) =>
        await http.WithBearer(token).PostAsJsonAsync(Routes.Clubs, request, cancellationToken);

    /// <summary>Registers an account and creates a club for it: the result is an owner session.</summary>
    public static async Task<SessionResponse> RegisterWithClubAsync(
        this HttpClient http, CancellationToken cancellationToken, string? name = null, string? slug = null)
    {
        var account = await http.RegisterAsync(cancellationToken);
        var request = new CreateClubRequest(name ?? DefaultClubName, slug ?? NewSlug(), null);
        var response = await http.CreateClubAsync(account.Token, request, cancellationToken);
        http.WithBearer(null);
        return await ReadSessionAsync(response, cancellationToken);
    }

    private static async Task<SessionResponse> ReadSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(cancellationToken))!;
    }
}
