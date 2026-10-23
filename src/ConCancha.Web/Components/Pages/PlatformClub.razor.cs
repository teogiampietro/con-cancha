using ConCancha.Contracts.Platform;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class PlatformClub : PlatformPageBase
{
    private const int FirstPage = 1;

    private ClubUsageDetailDto? _detail;
    private PlatformCustomersPageDto? _customers;
    private (Guid ClubId, PlatformPeriod Period)? _loaded;
    private int _latestRequest;
    private PlatformPeriod _period = new(DateOnly.MinValue, DateOnly.MinValue);
    private DateOnly _today;
    private string _search = string.Empty;

    [Parameter] public Guid ClubId { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "desde")] public string? DesdeQuery { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "hasta")] public string? HastaQuery { get; set; }

    [Inject] private TimeProvider Clock { get; set; } = null!;

    private int Peak => Math.Max(1, _detail?.Series.Max(bucket => bucket.Bookings) ?? 0);

    private int PageCount => _customers is null ? 1 : Math.Max(1, (int)Math.Ceiling(_customers.Total / (double)_customers.PageSize));

    protected override async Task OnParametersSetAsync()
    {
        _today = PlatformPeriods.Today(Clock);
        _period = PlatformPeriods.Parse(DesdeQuery, HastaQuery, _today);
        if (_loaded == (ClubId, _period)) return;

        _loaded = (ClubId, _period);
        _search = string.Empty;
        var request = ++_latestRequest;
        var detail = await Api.GetPlatformClubAsync(ClubId, _period, ComponentToken);
        if (request != _latestRequest || !await SucceededAsync(detail, ComponentToken)) return;

        _detail = detail.Value;
        await LoadCustomersAsync(FirstPage);
    }

    private Task ReloadCustomersAsync() => LoadCustomersAsync(FirstPage);

    private async Task LoadCustomersAsync(int page)
    {
        var request = ++_latestRequest;
        var result = await Api.GetPlatformCustomersAsync(ClubId, _period, _search.Trim(), page, ComponentToken);
        if (request != _latestRequest) return;

        if (await SucceededAsync(result, ComponentToken)) _customers = result.Value;
    }
}
