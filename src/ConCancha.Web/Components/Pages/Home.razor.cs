using ConCancha.Web.Services.Landing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components.Pages;

public partial class Home
{
    private const string NotifySubject = "Avisame cuando esté listo el chatbot de ConCancha";
    private const string ClosingSectionId = "cierre";
    private const string ChatDemoId = "chat-demo";
    private const string RevealOnViewFunction = "concancha.revealOnView";

    private LandingAudience _audience = LandingAudience.Club;
    private string _notifyContact = string.Empty;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime Js { get; set; } = default!;

    private IReadOnlyList<LandingItem> VisibleFeatures =>
        _audience == LandingAudience.Club ? LandingContent.ClubFeatures : LandingContent.PlayerFeatures;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await Js.InvokeVoidAsync(RevealOnViewFunction, ComponentToken, ChatDemoId);
        await Js.InvokeVoidAsync(RevealOnViewFunction, ComponentToken, ClosingSectionId);
    }

    private string AudienceTabClass(LandingAudience audience) => audience == _audience ? "on" : string.Empty;

    private void SelectAudience(LandingAudience audience) => _audience = audience;

    private void NotifyWhenReady()
    {
        if (string.IsNullOrWhiteSpace(_notifyContact)) return;

        var body = Uri.EscapeDataString($"Avisame cuando esté listo. Mi contacto: {_notifyContact.Trim()}");
        Navigation.NavigateTo($"mailto:{LandingContent.NotifyContactAddress}?subject={Uri.EscapeDataString(NotifySubject)}&body={body}");
    }
}
