using ConCancha.Web.Services.Landing;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class LandingIcon
{
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;
}
