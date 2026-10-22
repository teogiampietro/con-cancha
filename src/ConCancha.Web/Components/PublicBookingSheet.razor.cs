using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Public;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ConCancha.Web.Components;

public partial class PublicBookingSheet
{
    private const string ClipboardWriteFunction = "navigator.clipboard.writeText";

    private IReadOnlyDictionary<string, string[]>? _fieldErrors;
    private PublicBookingCreatedDto? _created;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isCopied;

    [Inject] private ApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private VisitorTokenStore Visitor { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Parameter, EditorRequired] public string Slug { get; set; } = string.Empty;
    [Parameter, EditorRequired] public PublicClubDto Club { get; set; } = null!;
    [Parameter, EditorRequired] public PublicCourtDto Court { get; set; } = null!;
    [Parameter, EditorRequired] public PublicSlotDto Slot { get; set; } = null!;
    [Parameter] public DateOnly Date { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private string ManagementPath => PageRoutes.PublicBooking(Slug, _created!.ManagementToken);

    private string ManagementLink => Navigation.ToAbsoluteUri(ManagementPath).ToString();

    private string? GetFieldError(string field) =>
        _fieldErrors is not null && _fieldErrors.TryGetValue(field, out var messages) ? messages.FirstOrDefault() : null;

    private async Task BookAsync(CancellationToken cancellationToken)
    {
        _isBusy = true;
        var request = new PublicBookingRequest(
            Court.Id, Date, Slot.StartMinuteOfDay, _name.Trim(), _phone.Trim(), string.IsNullOrWhiteSpace(_email) ? null : _email.Trim());
        var visitorToken = await Visitor.GetOrCreateAsync(cancellationToken);
        var result = await Api.BookPublicAsync(Slug, request, visitorToken, cancellationToken);
        _isBusy = false;

        _fieldErrors = result.FieldErrors?.ToDictionary(error => error.Key.ToLowerInvariant(), error => error.Value);
        _errorMessage = result.IsSuccess || _fieldErrors is not null ? null : result.GeneralMessage;
        if (result.IsSuccess) _created = result.Value;
    }

    private async Task CopyLinkAsync(CancellationToken cancellationToken)
    {
        await Js.InvokeVoidAsync(ClipboardWriteFunction, cancellationToken, ManagementLink);
        _isCopied = true;
    }
}
