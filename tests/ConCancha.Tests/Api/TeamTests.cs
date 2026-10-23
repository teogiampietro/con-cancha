using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

public class TeamTests(ApiFactory api) : ApiTestBase(api)
{
    private static readonly Regex TokenInLink = new(@"token=([^\s&]+)");
    private static readonly string AcceptLinkPrefix = $"{ApiFactory.WebBaseUrl}/panel/invitacion";

    [Fact]
    public async Task Invite_NewPerson_ListsThemAsInvitedAndMailsTheLink()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();

        var response = await InviteAsync(owner.Token, email);
        var team = await GetTeamAsync(owner.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(TeamMemberStatus.Invited, team.Single(m => m.Email == email).Status);
        Assert.StartsWith(AcceptLinkPrefix, Api.Emails.Sent.Last(m => m.To == email).Body.Split('\n')[1]);
    }

    [Fact]
    public async Task AcceptInvitation_CreatesStaffWhoSeesThePanelButNotTheTeam()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);

        var accepted = await AcceptAsync(email, ExtractToken(email));
        var session = (await accepted.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;

        Assert.Equal(nameof(ClubRole.Staff), session.Role);
        Assert.Equal(owner.Club!.Id, session.Club!.Id);
        Assert.Equal(HttpStatusCode.OK, (await Http.WithBearer(session.Token).GetAsync(Routes.CurrentClub, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Http.WithBearer(session.Token).GetAsync(Routes.CurrentClubTeam, Ct)).StatusCode);
        Assert.Equal(TeamMemberStatus.Active, (await GetTeamAsync(owner.Token)).Single(m => m.Email == email).Status);
    }

    [Fact]
    public async Task AcceptInvitation_LoginWorksAfterwards()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);
        await AcceptAsync(email, ExtractToken(email));

        var login = await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_ExpiredLinkIsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);
        await ExpireInvitationsAsync(email);

        var accepted = await AcceptAsync(email, ExtractToken(email));

        Assert.Equal(HttpStatusCode.BadRequest, accepted.StatusCode);
        Assert.DoesNotContain(await GetTeamAsync(owner.Token), m => m.Email == email);
    }

    [Fact]
    public async Task AcceptInvitation_LinkWorksOnlyOnce()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);
        var token = ExtractToken(email);

        await AcceptAsync(email, token);
        var again = await AcceptAsync(email, token);

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_ForgedTokenOrOtherMailIsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);

        Assert.Equal(HttpStatusCode.BadRequest, (await AcceptAsync(email, "falso")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AcceptAsync(ApiClientExtensions.NewEmail(), ExtractToken(email))).StatusCode);
    }

    [Fact]
    public async Task Invite_AgainRevokesThePreviousLink()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, email);
        var firstToken = ExtractToken(email);
        await InviteAsync(owner.Token, email);

        Assert.Equal(HttpStatusCode.BadRequest, (await AcceptAsync(email, firstToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AcceptAsync(email, ExtractToken(email))).StatusCode);
        Assert.Single(await GetTeamAsync(owner.Token), m => m.Email == email);
    }

    [Fact]
    public async Task Invite_AccountFromAnotherClubJoinsDirectlyWithoutSeeingTheOther()
    {
        var clubA = await Http.RegisterWithClubAsync(Ct, "Club A");
        var clubB = await Http.RegisterWithClubAsync(Ct, "Club B");

        var response = await InviteAsync(clubA.Token, clubB.User.Email);

        var added = (await response.Content.ReadFromJsonAsync<ClubMemberDto>(Ct))!;
        Assert.Equal(TeamMemberStatus.Active, added.Status);
        Assert.Equal(nameof(ClubRole.Staff), added.Role);
        Assert.Contains(await GetTeamAsync(clubA.Token), m => m.Email == clubB.User.Email && m.Role == nameof(ClubRole.Staff));
        Assert.DoesNotContain(await GetTeamAsync(clubB.Token), m => m.Email == clubA.User.Email);
    }

    [Fact]
    public async Task Invite_PersonAlreadyInTheTeamIsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await InviteAsync(owner.Token, owner.User.Email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Invite_MoreThanTheLimitIsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        for (var i = 0; i < TeamPolicy.MaxStaffPerClub; i++)
            Assert.Equal(HttpStatusCode.Created, (await InviteAsync(owner.Token, ApiClientExtensions.NewEmail())).StatusCode);

        var overLimit = await InviteAsync(owner.Token, ApiClientExtensions.NewEmail());

        Assert.Equal(HttpStatusCode.Conflict, overLimit.StatusCode);
    }

    [Fact]
    public async Task Invite_ExpiredInvitationsDoNotCountTowardsTheLimit()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var stale = ApiClientExtensions.NewEmail();
        await InviteAsync(owner.Token, stale);
        await ExpireInvitationsAsync(stale);
        for (var i = 0; i < TeamPolicy.MaxStaffPerClub; i++)
            await InviteAsync(owner.Token, ApiClientExtensions.NewEmail());

        Assert.DoesNotContain(await GetTeamAsync(owner.Token), m => m.Email == stale);
        Assert.Equal(TeamPolicy.MaxStaffPerClub + 1, (await GetTeamAsync(owner.Token)).Count);
    }

    [Fact]
    public async Task Invite_StaffCannotInvite()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);

        var response = await InviteAsync(staff.Token, ApiClientExtensions.NewEmail());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invite_InvalidMailIsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, (await InviteAsync(owner.Token, "no-es-un-mail")).StatusCode);
    }

    [Fact]
    public async Task RemoveMember_TakesAccessAwayEvenWithTheOldToken()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);
        var member = (await GetTeamAsync(owner.Token)).Single(m => m.Role == nameof(ClubRole.Staff));

        var removal = await Http.WithBearer(owner.Token).DeleteAsync($"{Routes.CurrentClubTeam}/{member.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, removal.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.WithBearer(staff.Token).GetAsync(Routes.CurrentClub, Ct)).StatusCode);
        Assert.DoesNotContain(await GetTeamAsync(owner.Token), m => m.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMember_TheOwnerCannotBeRemoved()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var ownerMember = (await GetTeamAsync(owner.Token)).Single();

        var removal = await Http.WithBearer(owner.Token).DeleteAsync($"{Routes.CurrentClubTeam}/{ownerMember.Id}", Ct);

        Assert.Equal(HttpStatusCode.Conflict, removal.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_OfAnotherClubIsNotFound()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        await CreateStaffSessionAsync(b.Club!.Id);
        var staffOfB = (await GetTeamAsync(b.Token)).Single(m => m.Role == nameof(ClubRole.Staff));

        var removal = await Http.WithBearer(a.Token).DeleteAsync($"{Routes.CurrentClubTeam}/{staffOfB.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, removal.StatusCode);
        Assert.Contains(await GetTeamAsync(b.Token), m => m.Id == staffOfB.Id);
    }

    [Fact]
    public async Task RevokeInvitation_KillsTheLink_AndOtherClubsCannotRevokeIt()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(a.Token, email);
        var invitation = (await GetTeamAsync(a.Token)).Single(m => m.Email == email);

        var foreign = await Http.WithBearer(b.Token).DeleteAsync($"{Routes.TeamInvitations}/{invitation.Id}", Ct);
        var own = await Http.WithBearer(a.Token).DeleteAsync($"{Routes.TeamInvitations}/{invitation.Id}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AcceptAsync(email, ExtractToken(email))).StatusCode);
    }

    [Fact]
    public async Task GetTeam_EachClubOnlySeesItsOwnInvitations()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        var email = ApiClientExtensions.NewEmail();
        await InviteAsync(a.Token, email);

        Assert.Contains(await GetTeamAsync(a.Token), m => m.Email == email);
        Assert.DoesNotContain(await GetTeamAsync(b.Token), m => m.Email == email);
    }

    [Fact]
    public async Task GlobalFilter_InvitationsOfOtherClubsAreInvisible()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        await InviteAsync(a.Token, ApiClientExtensions.NewEmail());

        using var scope = Api.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Application.Abstractions.ITenantContext>().SetClub(b.Club!.Id);
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();

        Assert.Empty(await db.ClubInvitations.ToListAsync(Ct));
        Assert.NotEmpty(await db.ClubInvitations.IgnoreQueryFilters().ToListAsync(Ct));
    }

    private Task<HttpResponseMessage> InviteAsync(string token, string email) =>
        Http.WithBearer(token).PostAsJsonAsync(Routes.TeamInvitations, new InviteTeamMemberRequest(email), Ct);

    private Task<HttpResponseMessage> AcceptAsync(string email, string token) =>
        Http.WithBearer(null).PostAsJsonAsync(
            Routes.AcceptInvitation, new AcceptInvitationRequest(email, token, ApiClientExtensions.ValidPassword), Ct);

    private async Task<IReadOnlyList<ClubMemberDto>> GetTeamAsync(string token) =>
        (await Http.WithBearer(token).GetFromJsonAsync<List<ClubMemberDto>>(Routes.CurrentClubTeam, Ct))!;

    private string ExtractToken(string email) =>
        TokenInLink.Match(Api.Emails.Sent.Last(m => m.To == email).Body).Groups[1].Value;

    private async Task ExpireInvitationsAsync(string email)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        await db.ClubInvitations.IgnoreQueryFilters().Where(i => i.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(i => i.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)), Ct);
    }
}
