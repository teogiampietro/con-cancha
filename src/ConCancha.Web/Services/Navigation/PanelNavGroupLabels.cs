namespace ConCancha.Web.Services.Navigation;

public static class PanelNavGroupLabels
{
    public static string? Get(PanelNavGroup group) => group switch
    {
        PanelNavGroup.Today => "Hoy",
        PanelNavGroup.Club => "El club",
        PanelNavGroup.Grow => "Crecer",
        _ => null
    };
}
