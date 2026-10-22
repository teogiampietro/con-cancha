namespace ConCancha.Web.Services.ClubSettings;

public static class SettingsTabs
{
    public static IReadOnlyList<(SettingsTab Tab, string Label)> All { get; } =
    [
        (SettingsTab.Brand, "Marca"),
        (SettingsTab.Sections, "Secciones"),
        (SettingsTab.Cancellation, "Cancelación"),
        (SettingsTab.Account, "Tu cuenta")
    ];
}
