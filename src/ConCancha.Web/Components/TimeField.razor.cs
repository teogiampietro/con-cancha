using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

/// <summary>A time input bound to an "HH:mm" string. <c>@bind</c> does not work with type="time" and strings.</summary>
public partial class TimeField
{
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string? Id { get; set; }

    private Task OnChanged(ChangeEventArgs args) => ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
