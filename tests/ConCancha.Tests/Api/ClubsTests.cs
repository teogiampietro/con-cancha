using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Common;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConCancha.Tests.Api;

public class ClubsTests(ApiFactory api) : ApiTestBase(api)
{
    private const string UnknownSlug = "no-existe-este-club";
    private const int AuthLimitPerMinute = 3;
    private const int AttemptsAboveLimit = 5;

    [Fact]
    public async Task CreateClub_WithoutSlug_BuildsItFromNameAndMakesUserOwner()
    {
        var account = await Http.RegisterAsync(Ct);
        var name = "Club Pádel " + Guid.NewGuid().ToString("N")[..6];

        var response = await Http.CreateClubAsync(account.Token, new CreateClubRequest(name, null, "221 555 1234"), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;
        Assert.StartsWith("club-padel-", session.Club!.Slug);
        Assert.Equal("221 555 1234", session.Club.Phone);
        Assert.Equal(nameof(ClubRole.Owner), session.Role);
        Assert.Equal(ClubDefaults.OpeningMinuteOfDay, session.Club.OpeningMinuteOfDay);
        Assert.Equal(TimeConstants.MinutesPerDay, session.Club.ClosingMinuteOfDay);
    }

    [Fact]
    public async Task CreateClub_NewSessionCarriesClubAndAllowsFetchingIt()
    {
        var session = await Http.RegisterWithClubAsync(Ct, "Los Pinos");

        var response = await Http.WithBearer(session.Token).GetAsync(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Los Pinos", (await response.Content.ReadFromJsonAsync<ClubDto>(Ct))!.Name);
    }

    [Fact]
    public async Task CreateClub_WithoutAuthentication_Returns401()
    {
        var response = await Http.WithBearer(null).PostAsJsonAsync(Routes.Clubs, new CreateClubRequest("X club", null, null), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("panel", "reservado")]
    [InlineData("ab", "al menos")]
    [InlineData("Con Espacios", "Solo letras")]
    public async Task CreateClub_WithInvalidSlug_Returns400(string slug, string expectedReason)
    {
        var account = await Http.RegisterAsync(Ct);

        var response = await Http.CreateClubAsync(account.Token, new CreateClubRequest("Un club", slug, null), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains(expectedReason, problem!.Errors["slug"][0]);
    }

    [Fact]
    public async Task CreateClub_WithTakenSlug_Returns409()
    {
        var slug = ApiClientExtensions.NewSlug();
        await Http.RegisterWithClubAsync(Ct, slug: slug);
        var other = await Http.RegisterAsync(Ct);

        var response = await Http.CreateClubAsync(other.Token, new CreateClubRequest("Otro club", slug.ToUpperInvariant(), null), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateClub_AccountThatAlreadyHasOne_Returns409()
    {
        var session = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.CreateClubAsync(
            session.Token, new CreateClubRequest("Segundo club", ApiClientExtensions.NewSlug(), null), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateClub_ConcurrentRequestsWithSameSlug_LeaveSingleClub()
    {
        var slug = ApiClientExtensions.NewSlug();
        var accounts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Api.CreateClient().RegisterAsync(Ct)));

        var responses = await Task.WhenAll(accounts.Select(account =>
            Api.CreateClient().CreateClubAsync(account.Token, new CreateClubRequest("Club carrera", slug, null), Ct)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task GetSlugAvailability_DistinguishesFreeReservedAndTaken()
    {
        var taken = ApiClientExtensions.NewSlug();
        await Http.RegisterWithClubAsync(Ct, slug: taken);

        var free = await GetAvailabilityAsync(ApiClientExtensions.NewSlug());
        var reserved = await GetAvailabilityAsync("panel");
        var occupied = await GetAvailabilityAsync(taken);

        Assert.True(free.IsAvailable);
        Assert.False(reserved.IsAvailable);
        Assert.Contains("reservado", reserved.Reason);
        Assert.False(occupied.IsAvailable);
        Assert.Contains("otro club", occupied.Reason);
    }

    [Fact]
    public async Task GetPublicClub_ResolvesBySlugWithoutSignIn()
    {
        var slug = ApiClientExtensions.NewSlug();
        await Http.RegisterWithClubAsync(Ct, "Club Público", slug);

        var found = await Http.WithBearer(null).GetAsync($"{Routes.PublicClubs}/{slug.ToUpperInvariant()}", Ct);
        var missing = await Http.GetAsync($"{Routes.PublicClubs}/{UnknownSlug}", Ct);

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal("Club Público", (await found.Content.ReadFromJsonAsync<PublicClubDto>(Ct))!.Name);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_AreRateLimitedByIp()
    {
        using var limited = Api.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:AuthPerMinute"] = AuthLimitPerMinute.ToString(),
            })));
        var http = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < AttemptsAboveLimit; attempt++)
            statuses.Add((await http.LoginAsync(ApiClientExtensions.NewEmail(), "x", Ct)).StatusCode);

        Assert.Equal(AuthLimitPerMinute, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(AttemptsAboveLimit - AuthLimitPerMinute, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    private async Task<SlugAvailabilityResponse> GetAvailabilityAsync(string slug) =>
        (await Http.GetFromJsonAsync<SlugAvailabilityResponse>($"{Routes.SlugAvailability}?slug={slug}", Ct))!;
}
