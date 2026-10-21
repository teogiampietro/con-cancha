namespace ConCancha.Web.Services;

public static class PageRoutes
{
    public const string Home = "/";
    public const string Login = "/panel/login";
    public const string Register = "/panel/registro";
    public const string Dashboard = "/panel";
    public const string Agenda = "/panel/agenda";
    public const string Courts = "/panel/canchas";
    public const string Setup = "/panel/configuracion-inicial";

    public static string PublicClub(string slug) => $"/{slug}";

    public static string PublicBooking(string slug, string token) => $"/{slug}/turno/{token}";
}
