using System.Net;
using ConCancha.Application.Abstractions;
using ConCancha.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

/// <summary>The platform panel and the club panel do not open each other's doors.</summary>
[Collection(ApiCollection.Name)]
public class PlatformAccessTests(ApiFactory api) : ApiTestBase(api)
{
    private const string Overview = "api/platform/overview";
    private const string ZeroFingerprint = "0000000000000000";

    [Fact]
    public async Task ClubSessionIsForbiddenOnThePlatformPanel()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var status = await StatusAsync(Overview, owner.Token);

        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task PlatformSessionIsForbiddenOnTheClubPanel()
    {
        var platform = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);

        var status = await StatusAsync(Routes.Courts, platform);

        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task TokenWithAnOldPasswordFingerprintIsForbidden()
    {
        using var scope = Api.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<ISessionTokenIssuer>();
        var stale = issuer.IssuePlatformAdmin(ZeroFingerprint).Value;
        var current = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(Overview, stale));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(Overview, current));
    }

    private async Task<HttpStatusCode> StatusAsync(string url, string token)
    {
        using var response = await Http.WithBearer(token).GetAsync(url, Ct);
        return response.StatusCode;
    }
}
