using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class Stat
{
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;

    /// <summary>Optional CSS tone: "bad" for money that is slipping away or owed.</summary>
    [Parameter] public string? Tone { get; set; }
}
