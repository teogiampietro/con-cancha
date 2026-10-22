using ConCancha.Web.Services.Navigation;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class Icon
{
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;
}
