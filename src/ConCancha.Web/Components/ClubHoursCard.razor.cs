using ConCancha.Contracts.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ClubHoursCard : CancellableComponentBase
{
    private string _opening = string.Empty;
    private string _closing = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter] public int OpeningMinuteOfDay { get; set; }
    [Parameter] public int ClosingMinuteOfDay { get; set; }
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    protected override void OnInitialized()
    {
        _opening = DisplayFormat.Time(OpeningMinuteOfDay);
        _closing = DisplayFormat.Time(ClosingMinuteOfDay);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        _isSaved = false;
        if (!DisplayFormat.TryParseTimeInput(_opening, isEndOfDay: false, out var opening)
            || !DisplayFormat.TryParseTimeInput(_closing, isEndOfDay: true, out var closing))
        {
            _errorMessage = "Poné la hora de apertura y de cierre.";
            return;
        }

        _isBusy = true;
        var result = await Api.UpdateClubHoursAsync(new UpdateClubHoursRequest(opening, closing), cancellationToken);
        _isBusy = false;
        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        _isSaved = result.IsSuccess;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }
}
