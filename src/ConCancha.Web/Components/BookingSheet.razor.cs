using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Customers;
using ConCancha.Domain.Bookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class BookingSheet
{
    private static readonly IReadOnlyList<PaymentMethod> PaymentMethods = Enum.GetValues<PaymentMethod>();

    private bool _isBlocking;
    private string _name = string.Empty;
    private string _phone = string.Empty;
    private decimal? _price;
    private bool _isPaid;
    private PaymentMethod _method = PaymentMethod.Cash;
    private string _reason = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public CourtDto Court { get; set; } = null!;
    [Parameter, EditorRequired] public AgendaSlotDto Slot { get; set; } = null!;
    [Parameter] public DateOnly Date { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    protected override void OnInitialized() => _price = DisplayFormat.Normalize(Slot.Price);

    private void ChooseBlock(bool isBlocking)
    {
        _isBlocking = isBlocking;
        _errorMessage = null;
    }

    private void OnCustomerPicked(CustomerSuggestionDto customer)
    {
        if (!string.IsNullOrEmpty(customer.Phone)) _phone = customer.Phone;
    }

    private async Task ReserveAsync(CancellationToken cancellationToken)
    {
        var request = new CreateBookingRequest(
            Court.Id, Date, Slot.StartMinuteOfDay, _name.Trim(), _phone.Trim(), _price, _isPaid, _isPaid ? _method : null);
        await SaveAsync(() => Api.CreateReservationAsync(request, cancellationToken));
    }

    private async Task BlockAsync(CancellationToken cancellationToken)
    {
        var request = new CreateBlockRequest(Court.Id, Date, Slot.StartMinuteOfDay, _reason.Trim());
        await SaveAsync(() => Api.CreateBlockAsync(request, cancellationToken));
    }

    private async Task SaveAsync(Func<Task<ApiResult<BookingDto>>> send)
    {
        _isBusy = true;
        var result = await send();
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnSaved.InvokeAsync();
    }
}
