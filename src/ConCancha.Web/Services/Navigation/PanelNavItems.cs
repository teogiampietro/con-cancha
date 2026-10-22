using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Components.Routing;

namespace ConCancha.Web.Services.Navigation;

/// <summary>Every panel destination and who sees it. A module that is not built yet has no row, so it never shows up.</summary>
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
        new("Configuración", PageRoutes.Settings, PanelIcons.Gear, PanelNavGroup.Club, OwnerOnly: true)
    ];

    public static IReadOnlyList<PanelNavItem> VisibleTo(string? role) =>
        All.Where(item => !item.OwnerOnly || IsOwner(role)).ToList();

    public static IReadOnlyList<PanelNavItem> BottomBarFor(string? role) =>
        VisibleTo(role).Where(item => item.InBottomBar).ToList();

    public static IReadOnlyList<PanelNavItem> MoreFor(string? role) =>
        VisibleTo(role).Where(item => !item.InBottomBar).ToList();

    public static IReadOnlyList<IGrouping<PanelNavGroup, PanelNavItem>> GroupedFor(string? role) =>
        VisibleTo(role).GroupBy(item => item.Group).ToList();

    private static bool IsOwner(string? role) => role == nameof(ClubRole.Owner);
}
