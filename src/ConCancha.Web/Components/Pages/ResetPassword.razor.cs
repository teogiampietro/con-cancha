using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class ResetPassword : CancellableComponentBase
{
    private string _newPassword = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isDone;

    [Inject] private ApiClient Api { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "mail")] private string? Email { get; set; }

    [SupplyParameterFromQuery] private string? Token { get; set; }

    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.ResetPasswordAsync(Email!, Token!, _newPassword, cancellationToken);
        _isBusy = false;

        if (result.IsSuccess) _isDone = true;
        else _errorMessage = result.GetFirstFieldError(ApiFieldNames.Token) ?? result.GeneralMessage;
    }
}
