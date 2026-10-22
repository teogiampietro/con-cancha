using ConCancha.Domain.Clubs;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class BottomNav
{
    [Parameter] public string? Role { get; set; }
    [Parameter] public ClubModules Modules { get; set; } = ClubModules.All;
    [Parameter] public EventCallback OnMore { get; set; }
}
