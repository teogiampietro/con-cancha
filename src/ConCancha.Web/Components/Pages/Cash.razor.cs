using ConCancha.Contracts.Cash;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components.Pages;

public partial class Cash : PanelPageBase
{
    private const string PrintFunction = "concancha.print";

    private CashDayDto? _cash;
    private DateOnly _date;
    private DateOnly _today;
    private bool _isBusy;

    [Inject] private ClubClock Clock { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken)) return;

        _today = await Clock.GetTodayAsync(ComponentToken);
        _date = _today;
        await LoadAsync(ComponentToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetCashAsync(_date, cancellationToken);
        if (await SucceededAsync(result, cancellationToken)) _cash = result.Value;
    }

    private Task ChangeDayAsync(DateOnly date)
    {
        _date = date;
        return LoadAsync(ComponentToken);
    }

    private Task CloseAsync() => ChangeClosureAsync(() => Api.CloseCashAsync(_date, ComponentToken));

    private Task ReopenAsync() => ChangeClosureAsync(() => Api.ReopenCashAsync(_date, ComponentToken));

    private async Task ChangeClosureAsync(Func<Task<ApiResult<CashDayDto>>> send)
    {
        _isBusy = true;
        var result = await send();
        _isBusy = false;

        if (await SucceededAsync(result, ComponentToken)) _cash = result.Value;
    }

    private async Task PrintAsync() => await Js.InvokeVoidAsync(PrintFunction, ComponentToken);
}
