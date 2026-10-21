using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services;

namespace ConCancha.Web.Components.Pages;

public partial class Dashboard : PanelPageBase
{
    private ClubDto? _club;
    private IReadOnlyList<ClubMemberDto>? _team;

    private string RoleLabel => IsOwner ? "dueño" : "encargado";

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken)) return;
        if (!await LoadClubAsync(ComponentToken)) return;
        if (await RedirectToSetupIfPendingAsync(ComponentToken)) return;

        await LoadTeamIfOwnerAsync(ComponentToken);
    }

    private async Task<bool> LoadClubAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCurrentClubAsync(cancellationToken);
        if (!await SucceededAsync(result, cancellationToken)) return false;

        _club = result.Value;
        return true;
    }

    private async Task<bool> RedirectToSetupIfPendingAsync(CancellationToken cancellationToken)
    {
        if (!IsOwner) return false;

        var result = await Api.GetSetupProgressAsync(cancellationToken);
        if (!result.IsSuccess || result.Value!.IsCompleted) return false;

        Navigation.NavigateTo(PageRoutes.Setup, replace: true);
        return true;
    }

    private async Task LoadTeamIfOwnerAsync(CancellationToken cancellationToken)
    {
        if (!IsOwner) return;

        var result = await Api.GetCurrentClubTeamAsync(cancellationToken);
        if (result.IsSuccess) _team = result.Value;
    }
}
