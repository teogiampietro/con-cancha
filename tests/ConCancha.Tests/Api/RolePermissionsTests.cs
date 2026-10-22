using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ConCancha.Api.Controllers;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ConCancha.Tests.Api;

/// <summary>Phase 5: the role of the session decides what is reachable, endpoint by endpoint.</summary>
public class RolePermissionsTests(ApiFactory api) : ApiTestBase(api)
{
    private static readonly Guid AnyId = Guid.NewGuid();

    public static TheoryData<string, string> OwnerOnlyEndpoints => new()
    {
        { "PUT", Routes.ClubHours },
        { "GET", Routes.CurrentClubTeam },
        { "GET", Routes.Setup },
        { "PUT", Routes.Setup },
        { "POST", Routes.SetupComplete },
        { "POST", Routes.Courts },
        { "PUT", $"{Routes.Courts}/{AnyId}" },
        { "POST", $"{Routes.Courts}/{AnyId}/price-rules" },
        { "PUT", $"{Routes.PriceRules}/{AnyId}" },
        { "DELETE", $"{Routes.PriceRules}/{AnyId}" },
    };

    public static TheoryData<string, string> PanelEndpoints => new()
    {
        { "GET", Routes.CurrentClub },
        { "GET", Routes.Courts },
        { "GET", $"{Routes.Courts}/{AnyId}/price-rules" },
        { "GET", Routes.Agenda },
        { "POST", Routes.Bookings },
        { "POST", Routes.Blocks },
        { "GET", $"{Routes.Bookings}/{AnyId}" },
        { "POST", $"{Routes.Bookings}/{AnyId}/pay" },
        { "POST", $"{Routes.Bookings}/{AnyId}/played" },
        { "POST", $"{Routes.Bookings}/{AnyId}/no-show" },
        { "PUT", $"{Routes.Bookings}/{AnyId}/price" },
        { "POST", $"{Routes.Bookings}/{AnyId}/cancel" },
        { "GET", Routes.Reservations },
        { "GET", Routes.Customers },
        { "GET", Routes.CustomersExport },
        { "GET", $"{Routes.Cash}/2026-01-01" },
        { "GET", Routes.Reminders },
    };

    [Theory]
    [MemberData(nameof(OwnerOnlyEndpoints))]
    public async Task OwnerOnlyEndpoint_StaffIsForbidden_AnonymousIsUnauthorized(string method, string url)
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);

        Assert.Equal(HttpStatusCode.Forbidden, await StatusAsync(method, url, staff.Token));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(method, url, token: null));
    }

    [Theory]
    [MemberData(nameof(PanelEndpoints))]
    public async Task PanelEndpoint_StaffIsAllowedIn_AnonymousIsUnauthorized(string method, string url)
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);

        var staffStatus = await StatusAsync(method, url, staff.Token);

        Assert.NotEqual(HttpStatusCode.Forbidden, staffStatus);
        Assert.NotEqual(HttpStatusCode.Unauthorized, staffStatus);
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(method, url, token: null));
    }

    [Theory]
    [MemberData(nameof(PanelEndpoints))]
    public async Task PanelEndpoint_AccountWithoutClubIsForbidden(string method, string url)
    {
        var account = await Http.RegisterAsync(Ct);

        var status = await StatusAsync(method, url, account.Token);

        Assert.True(status is HttpStatusCode.Forbidden || url == Routes.CurrentClub, $"{method} {url} answered {status}");
    }

    [Fact]
    public void EveryAction_DeclaresItsAccessExplicitly()
    {
        var undeclared = ControllerActions()
            .Where(action => !HasAccessAttribute(action) && !HasAccessAttribute(action.DeclaringType!))
            .Select(action => $"{action.DeclaringType!.Name}.{action.Name}")
            .ToList();

        Assert.Empty(undeclared);
    }

    private static IEnumerable<MethodInfo> ControllerActions() =>
        typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ApiControllerBase)) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static bool HasAccessAttribute(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>().Any() || member.GetCustomAttributes<AllowAnonymousAttribute>().Any();

    private async Task<HttpStatusCode> StatusAsync(string method, string url, string? token)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
        using var client = Api.CreateClient().WithBearer(token);
        return (await client.SendAsync(request, Ct)).StatusCode;
    }
}
