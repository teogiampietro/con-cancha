using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ConCancha.Web.Components;

public partial class App
{
    // No prerender: the session lives in the browser and is read once the circuit already has JavaScript.
    private static readonly IComponentRenderMode NoPrerender = new InteractiveServerRenderMode(prerender: false);

    [CascadingParameter] private HttpContext? HttpContext { get; set; }
}
