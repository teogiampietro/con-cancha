using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services.ClubSettings;

namespace ConCancha.Web.Components.Pages;

public partial class Settings : PanelPageBase
{
    private static readonly IReadOnlyList<(SettingsTab Tab, string Label)> Tabs = SettingsTabs.All;

    private ClubDto? _club;
    private SettingsTab _tab = SettingsTab.Brand;

    protected override async Task OnInitializedAsync()
    {
        if (await EnterPanelAsync(ComponentToken, ownerOnly: true)) await LoadAsync(ComponentToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCurrentClubAsync(cancellationToken);
        if (await SucceededAsync(result, cancellationToken)) _club = result.Value;
    }

    private void SelectTab(SettingsTab tab) => _tab = tab;

    private async Task OnClubSavedAsync(ClubDto club)
    {
        _club = club;
        await Session.SaveAsync(Session.Current! with { Club = club }, ComponentToken);
    }
}
