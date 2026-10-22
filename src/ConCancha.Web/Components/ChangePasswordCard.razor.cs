using ConCancha.Contracts.Accounts;
using ConCancha.Domain.Accounts;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ChangePasswordCard : CancellableComponentBase
{
    private const string PasswordsDoNotMatchMessage = "Las contraseñas no coinciden.";

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _repeatedPassword = string.Empty;
    private string? _currentError;
    private string? _newError;
    private string? _repeatError;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearMessages();
        if (_newPassword != _repeatedPassword)
        {
            _repeatError = PasswordsDoNotMatchMessage;
            return;
        }

        _isBusy = true;
        var result = await Api.ChangePasswordAsync(new ChangePasswordRequest(_currentPassword, _newPassword), cancellationToken);
        _isBusy = false;

        if (result.IsSuccess) Reset();
        else ShowErrors(result);
    }

    private void ShowErrors(ApiResult<object> result)
    {
        _currentError = result.GetFirstFieldError(ApiFieldNames.CurrentPassword);
        _newError = result.GetFirstFieldError(ApiFieldNames.NewPassword);
        _errorMessage = _currentError is null && _newError is null ? result.GeneralMessage : null;
    }

    private void Reset()
    {
        _currentPassword = _newPassword = _repeatedPassword = string.Empty;
        _isSaved = true;
    }

    private void ClearMessages()
    {
        _isSaved = false;
        _currentError = _newError = _repeatError = _errorMessage = null;
    }
}
