using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

/// <summary>Base for the platform panel's pages. An expired or revoked session signs out; any other error is shown.</summary>
public abstract class PlatformPageBase : CancellableComponentBase
{
    [Inject] protected ApiClient Api { get; set; } = null!;
    [Inject] protected PlatformSession Session { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    protected string? ErrorText { get; set; }

    /// <summary>Returns whether the call succeeded.</summary>
    protected async Task<bool> SucceededAsync<T>(ApiResult<T> result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            ErrorText = null;
            return true;
        }

        if (result.StatusCode == StatusCodes.Status401Unauthorized) await SignOutAsync(cancellationToken);
        else ErrorText = result.GeneralMessage;
        return false;
    }

    protected async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await Session.ClearAsync(cancellationToken);
        Navigation.NavigateTo(PageRoutes.PlatformLogin, replace: true);
    }
}
