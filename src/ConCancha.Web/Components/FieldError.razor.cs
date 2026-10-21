using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class FieldError
{
    [Parameter] public string? Message { get; set; }
}
