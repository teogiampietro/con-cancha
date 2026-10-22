extern alias web;

using ConCancha.Domain.Clubs;
using web::ConCancha.Web.Services;
using web::ConCancha.Web.Services.Navigation;

namespace ConCancha.Tests.Domain;

public class PanelNavItemsTests
{
    private static readonly string Owner = nameof(ClubRole.Owner);
    private static readonly string Staff = nameof(ClubRole.Staff);

    private static IReadOnlyList<string> RoutesFor(string? role) => PanelNavItems.VisibleTo(role).Select(item => item.Route).ToList();

    [Fact]
    public void Owner_sees_courts_in_the_menu()
    {
        Assert.Contains(PageRoutes.Courts, RoutesFor(Owner));
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void Anyone_who_is_not_the_owner_does_not_see_courts(string? role)
    {
        Assert.DoesNotContain(PageRoutes.Courts, RoutesFor(role));
    }

    [Fact]
    public void Staff_sees_the_daily_screens()
    {
        string[] daily = [PageRoutes.Dashboard, PageRoutes.Agenda, PageRoutes.Reservations, PageRoutes.Reminders, PageRoutes.Cash, PageRoutes.Customers];

        Assert.Equal(daily.Order(), RoutesFor(Staff).Order());
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Staff")]
    public void Bottom_bar_has_home_agenda_and_cash_for_every_role(string role)
    {
        var routes = PanelNavItems.BottomBarFor(role).Select(item => item.Route);

        Assert.Equal([PageRoutes.Dashboard, PageRoutes.Agenda, PageRoutes.Cash], routes);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Staff")]
    public void More_sheet_lists_exactly_what_the_bottom_bar_leaves_out(string role)
    {
        var all = RoutesFor(role).Order();
        var split = PanelNavItems.BottomBarFor(role).Concat(PanelNavItems.MoreFor(role)).Select(item => item.Route).Order();

        Assert.Equal(all, split);
    }

    [Fact]
    public void Groups_without_visible_items_are_not_shown()
    {
        var groups = PanelNavItems.GroupedFor(Staff).Select(group => group.Key);

        Assert.DoesNotContain(PanelNavGroup.Grow, groups);
        Assert.Equal([PanelNavGroup.Home, PanelNavGroup.Today, PanelNavGroup.Club], groups);
    }

    [Fact]
    public void Home_matches_only_its_exact_route()
    {
        var home = PanelNavItems.VisibleTo(Owner).Single(item => item.Route == PageRoutes.Dashboard);

        Assert.Equal(Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All, home.Match);
    }

    [Fact]
    public void Routes_are_unique()
    {
        var routes = RoutesFor(Owner);

        Assert.Equal(routes.Count, routes.Distinct().Count());
    }
}
