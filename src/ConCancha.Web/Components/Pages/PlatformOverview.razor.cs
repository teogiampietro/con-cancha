using ConCancha.Contracts.Platform;
using ConCancha.Domain.Platform;
using ConCancha.Domain.Subscriptions;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class PlatformOverview : PlatformPageBase
{
    private enum ClubFilter
    {
        All,
        Active,
        New,
        Inactive,
    }

    private static readonly IReadOnlyList<ClubFilter> Filters = Enum.GetValues<ClubFilter>();

    private PlatformOverviewDto? _overview;
    private PlatformPeriod? _loaded;
    private int _latestRequest;
    private PlatformPeriod _period = new(DateOnly.MinValue, DateOnly.MinValue);
    private DateOnly _today;
    private ClubFilter _filter = ClubFilter.All;
    private string _search = string.Empty;
    private string? _notice;
    private readonly Dictionary<Guid, int> _extensionDays = [];

    private static int MinExtensionDays => SubscriptionRules.MinExtensionDays;

    private static int MaxExtensionDays => SubscriptionRules.MaxExtensionDays;

    [Parameter, SupplyParameterFromQuery(Name = "desde")] public string? DesdeQuery { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "hasta")] public string? HastaQuery { get; set; }

    [Inject] private TimeProvider Clock { get; set; } = null!;

    private IReadOnlyList<ClubUsageDto> VisibleClubs => (_overview?.Clubs ?? [])
        .Where(club => Matches(_filter, club.Activity) && MatchesSearch(club))
        .ToList();

    protected override async Task OnParametersSetAsync()
    {
        _today = PlatformPeriods.Today(Clock);
        _period = PlatformPeriods.Parse(DesdeQuery, HastaQuery, _today);
        if (_loaded == _period) return;

        _loaded = _period;
        await LoadOverviewAsync();
    }

    private int ExtensionDaysOf(Guid clubId) => _extensionDays.GetValueOrDefault(clubId, SubscriptionRules.TrialDays);

    private void SetExtensionDays(Guid clubId, int days) => _extensionDays[clubId] = days;

    private async Task ExtendTrialAsync(Guid clubId)
    {
        var days = ExtensionDaysOf(clubId);
        var name = NameOf(clubId);
        var result = await Api.ExtendTrialAsync(clubId, days, ComponentToken);
        if (await SucceededAsync(result, ComponentToken))
            await ReloadAsync($"Le sumaste {PlatformFormat.Days(days)} de prueba a {name}.");
    }

    private async Task MarkPaidAsync(Guid clubId)
    {
        var name = NameOf(clubId);
        var result = await Api.MarkSubscriptionPaidAsync(clubId, ComponentToken);
        if (await SucceededAsync(result, ComponentToken))
            await ReloadAsync($"Registraste el pago de {name}: cubre {PlatformFormat.Days(SubscriptionRules.PaidDays)} más.");
    }

    private async Task SendRegularizationAsync(ClubUsageDto club)
    {
        var result = await Api.SendRegularizationEmailAsync(club.Id, ComponentToken);
        if (await SucceededAsync(result, ComponentToken))
            _notice = $"Enviamos el mail para regularizar la cuenta a {club.OwnerEmail}.";
    }

    private async Task ReloadAsync(string notice)
    {
        _notice = notice;
        await LoadOverviewAsync();
    }

    private async Task LoadOverviewAsync()
    {
        var request = ++_latestRequest;
        var result = await Api.GetPlatformOverviewAsync(_period, ComponentToken);
        if (request != _latestRequest) return;

        if (await SucceededAsync(result, ComponentToken)) _overview = result.Value;
    }

    private static bool CanRegularize(ClubUsageDto club) => SubscriptionRules.NeedsRegularization(club.Subscription.Status);

    private string NameOf(Guid clubId) => _overview?.Clubs.FirstOrDefault(club => club.Id == clubId)?.Name ?? "el club";

    private static bool Matches(ClubFilter filter, ClubActivity activity) => filter switch
    {
        ClubFilter.Active => activity == ClubActivity.Active,
        ClubFilter.New => activity == ClubActivity.New,
        ClubFilter.Inactive => activity == ClubActivity.Inactive,
        _ => true,
    };

    private bool MatchesSearch(ClubUsageDto club)
    {
        var text = _search.Trim();
        return text.Length == 0
            || club.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
            || (club.OwnerEmail?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private static string FilterLabel(ClubFilter filter) => filter switch
    {
        ClubFilter.Active => "Activos",
        ClubFilter.New => "Nuevos",
        ClubFilter.Inactive => "Inactivos",
        _ => "Todos",
    };
}
