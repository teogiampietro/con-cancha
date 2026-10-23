using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Accounts;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class AcceptInvitation : CancellableComponentBase
{
    private string _password = string.Empty;
    private string? _passwordError;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private CurrentSession Session { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "mail")] private string? Email { get; set; }

    [SupplyParameterFromQuery] private string? Token { get; set; }

    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        _passwordError = _errorMessage = null;
        _isBusy = true;
        var result = await Api.AcceptInvitationAsync(Email!, Token!, _password, cancellationToken);
        _isBusy = false;

        if (!result.IsSuccess)
        {
            ShowErrors(result);
            return;
        }

        await Session.SaveAsync(result.Value!, cancellationToken);
        Navigation.NavigateTo(PageRoutes.Dashboard);
    }

    private void ShowErrors(ApiResult<SessionResponse> result)
    {
        _passwordError = result.GetFirstFieldError(ApiFieldNames.Password);
        _errorMessage = _passwordError is null
            ? result.GetFirstFieldError(ApiFieldNames.Token) ?? result.GeneralMessage
            : null;
    }
}
