using ConCancha.Contracts.RecurringBookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class RecurringSheet : CancellableComponentBase
{
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _confirmingRemoval;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public Guid CourtId { get; set; }
    [Parameter, EditorRequired] public string CourtName { get; set; } = string.Empty;
    [Parameter, EditorRequired] public DayOfWeek Day { get; set; }
    [Parameter, EditorRequired] public int StartMinuteOfDay { get; set; }
    [Parameter] public RecurringBookingDto? Existing { get; set; }
    [Parameter] public EventCallback<RecurringBookingResultDto> OnSaved { get; set; }
    [Parameter] public EventCallback OnRemoved { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    protected override void OnInitialized()
    {
        _name = Existing?.CustomerName ?? string.Empty;
        _phone = Existing?.CustomerPhone ?? string.Empty;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        var phone = string.IsNullOrWhiteSpace(_phone) ? null : _phone.Trim();
        var result = Existing is null
            ? await Api.CreateRecurringBookingAsync(CourtId, new RecurringBookingRequest(Day, StartMinuteOfDay, _name.Trim(), phone), cancellationToken)
            : await Api.UpdateRecurringBookingAsync(CourtId, Existing.Id, new UpdateRecurringBookingRequest(_name.Trim(), phone), cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }

    private async Task RemoveAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        var result = await Api.DeleteRecurringBookingAsync(CourtId, Existing!.Id, cancellationToken);
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnRemoved.InvokeAsync();
    }
}
