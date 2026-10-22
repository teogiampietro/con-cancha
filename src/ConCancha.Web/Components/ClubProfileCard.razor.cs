using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ClubProfileCard : CancellableComponentBase
{
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string? _nameError;
    private string? _phoneError;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    protected override void OnInitialized()
    {
        _name = Club.Name;
        _phone = Club.Phone ?? string.Empty;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearMessages();
        _isBusy = true;
        var result = await Api.UpdateClubProfileAsync(new UpdateClubProfileRequest(_name, _phone), cancellationToken);
        _isBusy = false;

        if (result.IsSuccess) await ShowSavedAsync(result.Value!);
        else ShowErrors(result);
    }

    private async Task ShowSavedAsync(ClubDto club)
    {
        _isSaved = true;
        await OnSaved.InvokeAsync(club);
    }

    private void ShowErrors(ApiResult<ClubDto> result)
    {
        _nameError = result.GetFirstFieldError(ApiFieldNames.Name);
        _phoneError = result.GetFirstFieldError(ApiFieldNames.Phone);
        _errorMessage = _nameError is null && _phoneError is null ? result.GeneralMessage : null;
    }

    private void ClearMessages()
    {
        _isSaved = false;
        _nameError = _phoneError = _errorMessage = null;
    }
}
