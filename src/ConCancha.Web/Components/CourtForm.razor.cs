using ConCancha.Contracts.Courts;
using ConCancha.Domain.Courts;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class CourtForm : CancellableComponentBase
{
    private static readonly IReadOnlyList<Sport> AllSports = Enum.GetValues<Sport>();

    private string _name = string.Empty;
    private Sport _sport = Sport.Padel;
    private int _slotMinutes = SportDefaults.GetSlotMinutes(Sport.Padel);
    private bool _hasOwnHours;
    private string _opening = string.Empty;
    private string _closing = string.Empty;
    private bool _isActive = true;
    private decimal? _basePrice;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter] public CourtDto? Court { get; set; }
    [Parameter] public int ClubOpeningMinuteOfDay { get; set; }
    [Parameter] public int ClubClosingMinuteOfDay { get; set; }
    [Parameter] public EventCallback<CourtDto> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnInitialized()
    {
        _opening = DisplayFormat.Time(Court?.OpeningMinuteOfDay ?? ClubOpeningMinuteOfDay);
        _closing = DisplayFormat.Time(Court?.ClosingMinuteOfDay ?? ClubClosingMinuteOfDay);
        if (Court is null) return;

        _name = Court.Name;
        _sport = Court.Sport;
        _slotMinutes = Court.SlotMinutes;
        _hasOwnHours = Court.OpeningMinuteOfDay is not null;
        _isActive = Court.IsActive;
    }

    private void SelectSport(Sport sport)
    {
        _sport = sport;
        if (Court is null) _slotMinutes = SportDefaults.GetSlotMinutes(sport);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!TryBuildRequest(out var request)) return;

        _isBusy = true;
        var result = Court is null
            ? await Api.CreateCourtAsync(request, cancellationToken)
            : await Api.UpdateCourtAsync(Court.Id, request, cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }

    private bool TryBuildRequest(out CourtRequest request)
    {
        request = new CourtRequest(_name.Trim(), _sport, _slotMinutes, null, null, _isActive, Court is null ? _basePrice : null);
        if (Court is null && _basePrice is null)
        {
            _errorMessage = "Poné el precio base del turno.";
            return false;
        }

        if (!_hasOwnHours) return true;

        if (DisplayFormat.TryParseTimeInput(_opening, isEndOfDay: false, out var opening)
            && DisplayFormat.TryParseTimeInput(_closing, isEndOfDay: true, out var closing))
        {
            request = request with { OpeningMinuteOfDay = opening, ClosingMinuteOfDay = closing };
            return true;
        }

        _errorMessage = "Poné la hora de apertura y de cierre.";
        return false;
    }
}
