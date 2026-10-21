using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class Sheet
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
