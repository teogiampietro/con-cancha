using ConCancha.Web.Services;
using ConCancha.Web.Services.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components;

public partial class ShareLinkButton : CancellableComponentBase
{
    private const string ClipboardWriteFunction = "navigator.clipboard.writeText";
    private const string DefaultLabel = "Compartir link";
    private const string CopiedLabel = "Link copiado";
    private const string FailedLabel = "No se pudo copiar";

    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter, EditorRequired] public string Slug { get; set; } = string.Empty;
    [Parameter] public bool IconOnly { get; set; }
    [Parameter] public string CssClass { get; set; } = "btn sec sm";

    private string Label { get; set; } = DefaultLabel;

    private async Task CopyAsync(CancellationToken cancellationToken)
    {
        var link = Navigation.ToAbsoluteUri(PageRoutes.PublicClub(Slug)).ToString();
        try
        {
            await Js.InvokeVoidAsync(ClipboardWriteFunction, cancellationToken, link);
            Label = CopiedLabel;
        }
        catch (JSException)
        {
            Label = FailedLabel;
        }
    }
}
