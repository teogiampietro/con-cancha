using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class ErrorMessage
{
    [Parameter] public string? Message { get; set; }
}
