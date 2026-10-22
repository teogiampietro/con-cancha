using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ClubColorsCard : CancellableComponentBase
{
    private string _primary = string.Empty;
    private string _secondary = string.Empty;
    private string? _primaryError;
    private string? _secondaryError;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    private string PreviewStyle => ClubTheme.Style(_primary, _secondary);

    protected override void OnInitialized()
    {
        _primary = Club.PrimaryColor;
        _secondary = Club.SecondaryColor;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ClearMessages();
        _isBusy = true;
        var result = await Api.UpdateClubColorsAsync(new UpdateClubColorsRequest(_primary, _secondary), cancellationToken);
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
        _primaryError = result.GetFirstFieldError(ApiFieldNames.PrimaryColor);
        _secondaryError = result.GetFirstFieldError(ApiFieldNames.SecondaryColor);
        _errorMessage = _primaryError is null && _secondaryError is null ? result.GeneralMessage : null;
    }

    private void ClearMessages()
    {
        _isSaved = false;
        _primaryError = _secondaryError = _errorMessage = null;
    }
}
