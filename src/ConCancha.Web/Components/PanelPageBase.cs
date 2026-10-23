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

    /// <summary>
    /// Loads the session and redirects when this page is not for it. Returns whether the page can load its data.
    /// A club whose trial is over is sent to the subscription screen; that screen is the only one that skips the check.
    /// </summary>
    protected async Task<bool> EnterPanelAsync(CancellationToken cancellationToken, bool ownerOnly = false, bool checkSubscription = true)
    {
        await Session.LoadAsync(cancellationToken);
        if (Session.Current is null) return RedirectTo(PageRoutes.Login);
        if (!Session.HasClub) return RedirectTo(PageRoutes.Register);
        if (ownerOnly && !IsOwner) return RedirectTo(PageRoutes.Agenda);
        return !checkSubscription || await HasSubscriptionAccessAsync(cancellationToken);
    }

    /// <summary>Returns whether the call succeeded. An expired token signs out; a closed trial goes to the subscription screen; any other error is shown.</summary>
    protected async Task<bool> SucceededAsync<T>(ApiResult<T> result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess) return true;

        if (result.StatusCode == StatusCodes.Status401Unauthorized) await SignOutAsync(cancellationToken);
        else if (result.StatusCode == StatusCodes.Status402PaymentRequired) Navigation.NavigateTo(PageRoutes.Subscription, replace: true);
        else ErrorText = result.GeneralMessage;
        return false;
    }

    protected async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await Session.ClearAsync(cancellationToken);
        Navigation.NavigateTo(PageRoutes.Login);
    }

    private async Task<bool> HasSubscriptionAccessAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetClubSubscriptionAsync(cancellationToken);
        if (!result.IsSuccess) return await SucceededAsync(result, cancellationToken);
        if (result.Value!.HasAccess) return true;

        return RedirectTo(PageRoutes.Subscription);
    }

    private bool RedirectTo(string route)
    {
        Navigation.NavigateTo(route, replace: true);
        return false;
    }
}
