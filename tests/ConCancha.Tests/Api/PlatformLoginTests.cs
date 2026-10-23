using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

/// <summary>Every test that fails a sign-in ends with a successful one: that clears the address's failures.</summary>
[Collection(ApiCollection.Name)]
public class PlatformLoginTests(ApiFactory api) : ApiTestBase(api)
{
    private const string WrongPassword = "no-es-la-clave-del-admin";
    private const string InvalidCredentialsMessage = "Usuario o contraseña incorrectos.";
    private const int MaxFailures = 5;
    private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task ConfiguredUserAndPasswordGetAPlatformSessionWithoutClub()
    {
        var response = await LoginAsync(ApiFactory.PlatformAdminUser, ApiFactory.PlatformAdminPassword);

        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(Ct);
        Assert.Equal(PlatformRoles.Admin, session!.Role);
        Assert.Null(session.Club);
    }

    [Fact]
    public async Task WrongUserAndWrongPasswordGetTheSameAnswer()
    {
        var wrongUser = await LoginAsync("otra-persona", ApiFactory.PlatformAdminPassword);
        var wrongPassword = await LoginAsync(ApiFactory.PlatformAdminUser, WrongPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongUser.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await TitleAsync(wrongUser));
        Assert.Equal(InvalidCredentialsMessage, await TitleAsync(wrongPassword));
        await ClearFailuresAsync();
    }

    [Fact]
    public async Task AddressIsBlockedAfterFiveFailuresUntilTheBlockEnds()
    {
        for (var attempt = 0; attempt < MaxFailures; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(ApiFactory.PlatformAdminUser, WrongPassword)).StatusCode);

        var blocked = await LoginAsync(ApiFactory.PlatformAdminUser, ApiFactory.PlatformAdminPassword);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Contains("Demasiados intentos", await TitleAsync(blocked));

        Api.Clock.Advance(BlockDuration + TimeSpan.FromSeconds(1));
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(ApiFactory.PlatformAdminUser, ApiFactory.PlatformAdminPassword)).StatusCode);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task NoLogLineEverContainsThePasswordOrItsHash()
    {
        await LoginAsync(ApiFactory.PlatformAdminUser, WrongPassword);
        await LoginAsync(ApiFactory.PlatformAdminUser, ApiFactory.PlatformAdminPassword);

        var lines = Api.Logs.Messages.ToArray();
        Assert.NotEmpty(lines);
        Assert.All(lines, line =>
        {
            Assert.DoesNotContain(ApiFactory.PlatformAdminPassword, line);
            Assert.DoesNotContain(Api.PlatformAdminPasswordHash, line);
        });
    }

    private async Task<HttpResponseMessage> LoginAsync(string user, string password) =>
        await Http.PostAsJsonAsync("api/platform/auth/login", new PlatformLoginRequest(user, password), Ct);

    private async Task ClearFailuresAsync() =>
        (await LoginAsync(ApiFactory.PlatformAdminUser, ApiFactory.PlatformAdminPassword)).EnsureSuccessStatusCode();

    private static async Task<string?> TitleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
}
