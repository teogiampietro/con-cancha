using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

/// <summary>
/// Base for panel pages. Access control lives here and not in [Authorize]: the session is in the browser, so the
/// server does not know it on the first request. The API validates the token on every call.
/// </summary>
public abstract class PanelPageBase : CancellableComponentBase
{
    [Inject] protected ApiClient Api { get; set; } = null!;
    [Inject] protected CurrentSession Session { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected bool IsOwner => Session.Current?.Role == nameof(ClubRole.Owner);

    protected string? ErrorText { get; set; }

    /// <summary>Loads the session and redirects when this page is not for it. Returns whether the page can load its data.</summary>
    protected async Task<bool> EnterPanelAsync(CancellationToken cancellationToken, bool ownerOnly = false)
    {
        await Session.LoadAsync(cancellationToken);
        if (Session.Current is null) return RedirectTo(PageRoutes.Login);
        if (!Session.HasClub) return RedirectTo(PageRoutes.Register);
        return ownerOnly && !IsOwner ? RedirectTo(PageRoutes.Agenda) : true;
    }

    /// <summary>Returns whether the call succeeded. An expired token signs out; any other error is shown.</summary>
    protected async Task<bool> SucceededAsync<T>(ApiResult<T> result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess) return true;

        if (result.StatusCode == StatusCodes.Status401Unauthorized) await SignOutAsync(cancellationToken);
        else ErrorText = result.GeneralMessage;
        return false;
    }

    protected async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await Session.ClearAsync(cancellationToken);
        Navigation.NavigateTo(PageRoutes.Login);
    }

    private bool RedirectTo(string route)
    {
        Navigation.NavigateTo(route, replace: true);
        return false;
    }
}
