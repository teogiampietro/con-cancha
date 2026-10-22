using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class BrandLogo
{
    [Parameter] public bool OnDark { get; set; } = true;

    [Parameter] public bool ShowTagline { get; set; }
}
