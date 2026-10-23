using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class PlatformLogin : CancellableComponentBase
{
    private string _user = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private PlatformSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.LoginPlatformAsync(_user, _password, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.GeneralMessage;
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        Navigation.NavigateTo(PageRoutes.Platform);
    }
}
