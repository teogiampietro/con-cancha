using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Login : CancellableComponentBase
{
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private CurrentSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.LoginAsync(_email, _password, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            _errorMessage = result.GeneralMessage;
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        Navigation.NavigateTo(Session.HasClub ? PageRoutes.Dashboard : PageRoutes.Register);
    }
}
