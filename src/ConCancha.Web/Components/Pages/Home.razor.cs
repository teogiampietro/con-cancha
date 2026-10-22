using ConCancha.Web.Services.Landing;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components.Pages;

public partial class Home
{
    private const string NotifySubject = "Avisame cuando esté listo el chatbot de ConCancha";

    private LandingAudience _audience = LandingAudience.Club;
    private string _notifyContact = string.Empty;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private IReadOnlyList<LandingItem> VisibleFeatures =>
        _audience == LandingAudience.Club ? LandingContent.ClubFeatures : LandingContent.PlayerFeatures;

    private string PlanPriceLabel => LandingContent.Plan.Price ?? "[PRECIO A COMPLETAR]";

    private string PlanPeriodLabel => LandingContent.Plan.Period ?? "[periodicidad]";

    private bool IsPlanPending => LandingContent.Plan.Price is null;

    private string AudienceTabClass(LandingAudience audience) => audience == _audience ? "on" : string.Empty;

    private void SelectAudience(LandingAudience audience) => _audience = audience;

    private void NotifyWhenReady()
    {
        if (string.IsNullOrWhiteSpace(_notifyContact)) return;

        var body = Uri.EscapeDataString($"Avisame cuando esté listo. Mi contacto: {_notifyContact.Trim()}");
        Navigation.NavigateTo($"mailto:{LandingContent.NotifyContactAddress}?subject={Uri.EscapeDataString(NotifySubject)}&body={body}");
    }
}
