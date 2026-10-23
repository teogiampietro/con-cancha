using ConCancha.Domain.Subscriptions;
using ConCancha.Web.Services;

namespace ConCancha.Web.Components.Pages;

public partial class Subscription : PanelPageBase
{
    private const string NotPaidNotice = "Todavía no figura una suscripción paga para este club.";

    private bool _isChecked;
    private bool _isBusy;
    private string? _notice;

    private string Explanation => IsOwner
        ? $"La prueba de {SubscriptionRules.TrialDays} días de {ClubName} terminó y la cuenta no tiene una suscripción paga. Escribinos y te pasamos los datos para suscribirte."
        : $"La prueba de {ClubName} terminó y la cuenta no tiene una suscripción paga. Avisale al dueño del club para que la regularice.";

    private string ClubName => Session.Current?.Club?.Name ?? "el club";

    protected override async Task OnInitializedAsync()
    {
        if (!await EnterPanelAsync(ComponentToken, checkSubscription: false)) return;

        await OpenPanelIfPaidAsync(ComponentToken);
        _isChecked = true;
    }

    private async Task RetryAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        _notice = null;
        var open = await OpenPanelIfPaidAsync(cancellationToken);
        _isBusy = false;

        if (!open && ErrorText is null) _notice = NotPaidNotice;
    }

    /// <summary>Goes to the panel once the club may use it. Returns whether it may.</summary>
    private async Task<bool> OpenPanelIfPaidAsync(CancellationToken cancellationToken)
    {
        var result = await Api.GetClubSubscriptionAsync(cancellationToken);
        if (!await SucceededAsync(result, cancellationToken) || !result.Value!.HasAccess) return false;

        Navigation.NavigateTo(PageRoutes.Dashboard, replace: true);
        return true;
    }
}
