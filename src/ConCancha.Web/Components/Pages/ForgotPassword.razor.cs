using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class ForgotPassword : CancellableComponentBase
{
    private string _email = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSent;

    [Inject] private ApiClient Api { get; set; } = null!;

    private async Task SubmitAsync(CancellationToken cancellationToken)
    {
        _errorMessage = null;
        _isBusy = true;
        var result = await Api.RequestPasswordResetAsync(_email, cancellationToken);
        _isBusy = false;

        if (result.IsSuccess) _isSent = true;
        else _errorMessage = result.GeneralMessage;
    }
}
