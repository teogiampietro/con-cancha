using Microsoft.AspNetCore.Components.Routing;

namespace ConCancha.Web.Services.Navigation;

public sealed record PanelNavItem(
    string Label,
    string Route,
    string Icon,
    PanelNavGroup Group,
    bool OwnerOnly = false,
    bool InBottomBar = false,
    NavLinkMatch Match = NavLinkMatch.Prefix);
