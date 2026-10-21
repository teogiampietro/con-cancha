using System.Net.Http.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Support;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFactory api)
{
    protected ApiFactory Api { get; } = api;

    protected HttpClient Http { get; } = api.CreateClient();

    protected async Task<SessionResponse> CreateStaffSessionAsync(Guid clubId)
    {
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email);
        db.ClubMembers.Add(new ClubMember { ClubId = clubId, UserId = user!.Id, Role = ClubRole.Staff });
        await db.SaveChangesAsync(Ct);

        var login = await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct);
        return (await login.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;
    }

    // xunit 2.x exposes no per-test token, so tests use one that is never cancelled.
    protected static CancellationToken Ct => CancellationToken.None;
}
