namespace ConCancha.Web.Services;

public static class PageRoutes
{
    public const string Home = "/";
    public const string Login = "/panel/login";
    public const string Register = "/panel/registro";
    public const string RegisterVerifyPattern = "/panel/registro?mail={0}";
    public const string Dashboard = "/panel";
    public const string Agenda = "/panel/agenda";
    public const string Reservations = "/panel/reservas";
    public const string Reminders = "/panel/recordar";
    public const string Cash = "/panel/caja";
    public const string Customers = "/panel/clientes";
    public const string Courts = "/panel/canchas";
    public const string Team = "/panel/equipo";
    public const string AcceptInvitation = "/panel/invitacion";
    public const string Settings = "/panel/configuracion";
    public const string Subscription = "/panel/suscripcion";
    public const string Setup = "/panel/configuracion-inicial";
    public const string PlatformLogin = "/plataforma/ingresar";
    public const string Platform = "/plataforma";

    public static string PlatformClub(Guid clubId) => $"/plataforma/clubes/{clubId}";

    public const string ClubLogoPattern = "/club-logo/{slug}";

    public static string ClubLogo(string slug, string version) => $"/club-logo/{Uri.EscapeDataString(slug)}?v={Uri.EscapeDataString(version)}";

    public static string PublicClub(string slug) => $"/{slug}";

    public static string PublicBooking(string slug, string token) => $"/{slug}/turno/{token}";
}
