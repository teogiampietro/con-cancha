using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class BottomNav
{
    [Parameter] public string? Role { get; set; }
    [Parameter] public EventCallback OnMore { get; set; }
}
