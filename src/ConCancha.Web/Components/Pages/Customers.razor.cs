using ConCancha.Contracts.Customers;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components.Pages;

public partial class Customers : PanelPageBase
{
    private const string DownloadFunction = "concancha.downloadFile";
    private const string PrintFunction = "concancha.print";

    private IReadOnlyList<CustomerSummaryDto>? _customers;
    private string _search = string.Empty;
    private bool _onlyDebtors;
    private bool _onlyNoShows;
    private bool _isExporting;
    private int _loadVersion;

    [Inject] private IJSRuntime Js { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (await EnterPanelAsync(ComponentToken)) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        var result = await Api.GetCustomersAsync(_search, _onlyDebtors, _onlyNoShows, ComponentToken);
        if (version != _loadVersion) return;

        if (await SucceededAsync(result, ComponentToken)) _customers = result.Value;
    }

    private Task ToggleDebtorsAsync(ChangeEventArgs args)
    {
        _onlyDebtors = args.Value is true;
        return LoadAsync();
    }

    private Task ToggleNoShowsAsync(ChangeEventArgs args)
    {
        _onlyNoShows = args.Value is true;
        return LoadAsync();
    }

    private async Task ExportAsync()
    {
        _isExporting = true;
        var result = await Api.ExportCustomersAsync(_search, _onlyDebtors, _onlyNoShows, ComponentToken);
        _isExporting = false;
        if (!await SucceededAsync(result, ComponentToken)) return;

        var file = result.Value!;
        await Js.InvokeVoidAsync(DownloadFunction, ComponentToken, file.FileName, file.ContentType, file.Content);
    }

    private async Task PrintAsync() => await Js.InvokeVoidAsync(PrintFunction, ComponentToken);

    private static string GreetingFor(CustomerSummaryDto customer) =>
        $"Hola {customer.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? customer.Name}!";
}
