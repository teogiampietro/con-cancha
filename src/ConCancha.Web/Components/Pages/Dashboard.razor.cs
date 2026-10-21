using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Common;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Dashboard : CancellableComponentBase
{
    private ClubDto? _club;
    private IReadOnlyList<ClubMemberDto>? _team;
    private string? _errorMessage;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private CurrentSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool IsOwner => Session.Current?.Role == nameof(ClubRole.Owner);

    private string RoleLabel => IsOwner ? "dueño" : "encargado";

    // Access control lives here and not in [Authorize]: the session is in the browser, so the server
    // does not know it on the first request. The API validates the token on every call.
    protected override async Task OnInitializedAsync()
    {
        await Session.LoadAsync(ComponentToken);
        if (RedirectIfNoClubSession()) return;
        if (!await LoadClubAsync(ComponentToken)) return;

        await LoadTeamIfOwnerAsync(ComponentToken);
    }

    private bool RedirectIfNoClubSession()
    {
        if (Session.Current is null)
            Navigation.NavigateTo(PageRoutes.Login, replace: true);
        else if (!Session.HasClub)
            Navigation.NavigateTo(PageRoutes.Register, replace: true);
        else
            return false;

        return true;
    }

    private async Task<bool> LoadClubAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCurrentClubAsync(cancellationToken);
        if (result.IsSuccess)
        {
            _club = result.Value;
            return true;
        }

        // Expired or invalid token: sign out and log in again.
        if (result.StatusCode == StatusCodes.Status401Unauthorized) await SignOutAsync(cancellationToken);
        else _errorMessage = result.GeneralMessage;
        return false;
    }

    private async Task LoadTeamIfOwnerAsync(CancellationToken cancellationToken)
    {
        if (!IsOwner) return;

        var result = await Api.GetCurrentClubTeamAsync(cancellationToken);
        if (result.IsSuccess) _team = result.Value;
    }

    private async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await Session.ClearAsync(cancellationToken);
        Navigation.NavigateTo(PageRoutes.Login);
    }

    private static string FormatMinuteOfDay(int minuteOfDay)
    {
        var hour = minuteOfDay / TimeConstants.MinutesPerHour % TimeConstants.HoursPerDay;
        return $"{hour:00}:{minuteOfDay % TimeConstants.MinutesPerHour:00}";
    }
}
