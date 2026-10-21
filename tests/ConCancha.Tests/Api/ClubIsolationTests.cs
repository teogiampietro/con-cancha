using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ConCancha.Application.Abstractions;
using ConCancha.Contracts;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ConCancha.Tests.Api;

/// <summary>Phase 1 exit criterion: an owner signs in and only sees their own club's data.</summary>
public class ClubIsolationTests(ApiFactory api) : ApiTestBase(api)
{
    private const string TokenIssuerAndAudience = "concancha";
    private const string ForgingKey = "otra-clave-distinta-0123456789-0123456789-0123456789";
    private const string ArbitraryUserId = "cualquiera";

    [Fact]
    public async Task GetTeam_EachClubOnlyShowsItsOwnMembers()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A");
        var b = await Http.RegisterWithClubAsync(Ct, "Club B");

        var teamA = await Http.WithBearer(a.Token).GetFromJsonAsync<List<ClubMemberDto>>(Routes.CurrentClubTeam, Ct);
        var teamB = await Http.WithBearer(b.Token).GetFromJsonAsync<List<ClubMemberDto>>(Routes.CurrentClubTeam, Ct);

        Assert.Equal([a.User.Email], teamA!.Select(m => m.Email));
        Assert.Equal([b.User.Email], teamB!.Select(m => m.Email));
    }

    [Fact]
    public async Task GetCurrentClub_EachOwnerSeesTheirOwnClub()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A", ApiClientExtensions.NewSlug());
        var b = await Http.RegisterWithClubAsync(Ct, "Club B", ApiClientExtensions.NewSlug());

        Assert.Equal(a.Club!.Id, (await Http.WithBearer(a.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct))!.Id);
        Assert.Equal(b.Club!.Id, (await Http.WithBearer(b.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct))!.Id);
    }

    [Fact]
    public async Task GetTeam_TokenWithHandEditedClubIsRejected()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A");
        var b = await Http.RegisterWithClubAsync(Ct, "Club B");
        var forged = ForgeToken(a.User.Id, b.Club!.Id);

        var response = await Http.WithBearer(forged).GetAsync(Routes.CurrentClubTeam, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_SeesClubButNotTeam()
    {
        var owner = await Http.RegisterWithClubAsync(Ct, "Club con encargado");
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);
        await AddMemberAsync(owner.Club!.Id, email, ClubRole.Staff);

        var login = await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct);
        var session = (await login.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;

        Assert.Equal(nameof(ClubRole.Staff), session.Role);
        Assert.Equal(HttpStatusCode.OK, (await Http.WithBearer(session.Token).GetAsync(Routes.CurrentClub, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Http.WithBearer(session.Token).GetAsync(Routes.CurrentClubTeam, Ct)).StatusCode);
    }

    [Fact]
    public async Task AccountWithoutClub_SeesNothingOfThePanel()
    {
        var account = await Http.RegisterAsync(Ct);
        Http.WithBearer(account.Token);

        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync(Routes.CurrentClub, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Http.GetAsync(Routes.CurrentClubTeam, Ct)).StatusCode);
    }

    [Fact]
    public async Task Panel_WithoutToken_Returns401()
    {
        Http.WithBearer(null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.GetAsync(Routes.CurrentClub, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.GetAsync(Routes.CurrentClubTeam, Ct)).StatusCode);
    }

    [Fact]
    public async Task GlobalFilter_OnlyReturnsRowsOfCurrentClub()
    {
        var (clubA, clubB) = await CreateTwoClubsAsync();

        using var scopeA = Api.Services.CreateScope();
        var membersOfA = await ContextFor(scopeA, clubA).ClubMembers.ToListAsync(Ct);
        using var scopeB = Api.Services.CreateScope();
        var membersOfB = await ContextFor(scopeB, clubB).ClubMembers.ToListAsync(Ct);

        Assert.All(membersOfA, member => Assert.Equal(clubA, member.ClubId));
        Assert.All(membersOfB, member => Assert.Equal(clubB, member.ClubId));
        Assert.Single(membersOfA);
        Assert.Single(membersOfB);
    }

    [Fact]
    public async Task GlobalFilter_WithoutCurrentClub_ReturnsNothing()
    {
        await CreateTwoClubsAsync();

        using var scope = Api.Services.CreateScope();
        var db = ContextFor(scope, club: null);

        Assert.Empty(await db.ClubMembers.ToListAsync(Ct));
        Assert.NotEmpty(await db.ClubMembers.IgnoreQueryFilters().ToListAsync(Ct));
    }

    [Fact]
    public async Task SaveChanges_CannotWriteToAnotherClub()
    {
        var (clubA, clubB) = await CreateTwoClubsAsync();

        using var scope = Api.Services.CreateScope();
        var db = ContextFor(scope, clubA);
        db.ClubMembers.Add(new ClubMember { ClubId = clubB, UserId = ArbitraryUserId, Role = ClubRole.Staff });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task SaveChanges_WithCurrentClub_FillsInMissingClub()
    {
        var (clubA, _) = await CreateTwoClubsAsync();
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        using var scope = Api.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email);
        var db = ContextFor(scope, clubA);
        var member = new ClubMember { UserId = user!.Id, Role = ClubRole.Staff };
        db.ClubMembers.Add(member);
        await db.SaveChangesAsync(Ct);

        Assert.Equal(clubA, member.ClubId);
    }

    private async Task<(Guid ClubA, Guid ClubB)> CreateTwoClubsAsync()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        return (a.Club!.Id, b.Club!.Id);
    }

    private async Task AddMemberAsync(Guid clubId, string email, ClubRole role)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email);
        db.ClubMembers.Add(new ClubMember { ClubId = clubId, UserId = user!.Id, Role = role });
        await db.SaveChangesAsync(Ct);
    }

    private static ConCanchaDbContext ContextFor(IServiceScope scope, Guid? club)
    {
        if (club is { } id) scope.ServiceProvider.GetRequiredService<ITenantContext>().SetClub(id);
        return scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
    }

    private static string ForgeToken(string userId, Guid clubId) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            TokenIssuerAndAudience,
            TokenIssuerAndAudience,
            [
                new Claim(SessionClaims.UserId, userId),
                new Claim(SessionClaims.ClubId, clubId.ToString()),
                new Claim(SessionClaims.Role, nameof(ClubRole.Owner)),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ForgingKey)), SecurityAlgorithms.HmacSha256)));
}
