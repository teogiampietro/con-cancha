using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Components.Routing;

namespace ConCancha.Web.Services.Navigation;

/// <summary>
/// Every panel destination and who sees it. A module that is not built yet has no row, so it never shows up.
/// A row tied to an optional section (<see cref="PanelNavItem.Module"/>) also disappears when the club hides that section.
/// </summary>
public static class PanelNavItems
{
    private static readonly IReadOnlyList<PanelNavItem> All =
    [
        new("Inicio", PageRoutes.Dashboard, PanelIcons.Home, PanelNavGroup.Home, InBottomBar: true, Match: NavLinkMatch.All),
        new("Agenda", PageRoutes.Agenda, PanelIcons.Calendar, PanelNavGroup.Today, InBottomBar: true),
        new("Reservas", PageRoutes.Reservations, PanelIcons.List, PanelNavGroup.Today),
        new("Recordar turnos", PageRoutes.Reminders, PanelIcons.Bell, PanelNavGroup.Today),
        new("Caja", PageRoutes.Cash, PanelIcons.Wallet, PanelNavGroup.Today, InBottomBar: true),
        new("Canchas", PageRoutes.Courts, PanelIcons.Grid, PanelNavGroup.Club, OwnerOnly: true),
        new("Clientes", PageRoutes.Customers, PanelIcons.Users, PanelNavGroup.Club),
        new("Equipo", PageRoutes.Team, PanelIcons.UserPlus, PanelNavGroup.Club, OwnerOnly: true),
        new("Configuración", PageRoutes.Settings, PanelIcons.Gear, PanelNavGroup.Club, OwnerOnly: true)
    ];

    public static IReadOnlyList<PanelNavItem> VisibleTo(string? role, ClubModules modules = ClubModules.All) =>
        Filter(All, role, modules);

    public static IReadOnlyList<PanelNavItem> BottomBarFor(string? role, ClubModules modules = ClubModules.All) =>
        VisibleTo(role, modules).Where(item => item.InBottomBar).ToList();

    public static IReadOnlyList<PanelNavItem> MoreFor(string? role, ClubModules modules = ClubModules.All) =>
        VisibleTo(role, modules).Where(item => !item.InBottomBar).ToList();

    public static IReadOnlyList<IGrouping<PanelNavGroup, PanelNavItem>> GroupedFor(string? role, ClubModules modules = ClubModules.All) =>
        VisibleTo(role, modules).GroupBy(item => item.Group).ToList();

    public static IReadOnlyList<PanelNavItem> Filter(IEnumerable<PanelNavItem> items, string? role, ClubModules modules) =>
        items
            .Where(item => !item.OwnerOnly || IsOwner(role))
            .Where(item => item.Module is not { } module || modules.HasFlag(module))
            .ToList();

    private static bool IsOwner(string? role) => role == nameof(ClubRole.Owner);
}
