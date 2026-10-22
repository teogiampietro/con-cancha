using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class CancellationCard : CancellableComponentBase
{
    private int _windowHours;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    protected override void OnInitialized() => _windowHours = Club.CancellationWindowHours;

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        _isSaved = false;
        _isBusy = true;
        var result = await Api.UpdateClubCancellationAsync(new UpdateCancellationRequest(_windowHours), cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        _isSaved = result.IsSuccess;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }
}
