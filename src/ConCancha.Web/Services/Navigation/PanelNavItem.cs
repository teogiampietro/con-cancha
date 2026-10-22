using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Components.Routing;

namespace ConCancha.Web.Services.Navigation;

public sealed record PanelNavItem(
    string Label,
    string Route,
    string Icon,
    PanelNavGroup Group,
    bool OwnerOnly = false,
    ClubModules? Module = null,
    bool InBottomBar = false,
    NavLinkMatch Match = NavLinkMatch.Prefix);
