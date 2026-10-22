using ConCancha.Contracts.Customers;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ConCancha.Web.Components;

public partial class CustomerAutocomplete : CancellableComponentBase
{
    private const int MinQueryLength = 2;
    private const int DebounceMilliseconds = 200;
    private const string EscapeKey = "Escape";

    private IReadOnlyList<CustomerSuggestionDto> _suggestions = [];
    private int _version;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter] public string Id { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public int MaxLength { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public EventCallback<CustomerSuggestionDto> OnPicked { get; set; }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        var text = args.Value?.ToString() ?? string.Empty;
        await ValueChanged.InvokeAsync(text);
        await LoadSuggestionsAsync(text.Trim(), ++_version, ComponentToken);
    }

    private async Task LoadSuggestionsAsync(string query, int version, CancellationToken cancellationToken)
    {
        if (query.Length < MinQueryLength)
        {
            _suggestions = [];
            return;
        }

        await Task.Delay(DebounceMilliseconds, cancellationToken);
        if (version != _version) return;

        var result = await Api.GetCustomerSuggestionsAsync(query, cancellationToken);
        if (version == _version) _suggestions = result.IsSuccess ? result.Value! : [];
    }

    private async Task PickAsync(CustomerSuggestionDto suggestion)
    {
        _version++;
        _suggestions = [];
        await ValueChanged.InvokeAsync(suggestion.Name);
        await OnPicked.InvokeAsync(suggestion);
    }

    private void OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key != EscapeKey) return;

        _version++;
        _suggestions = [];
    }
}
