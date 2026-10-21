using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Bookings;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class BookingDetailSheet
{
    private static readonly IReadOnlyList<PaymentMethod> PaymentMethods = Enum.GetValues<PaymentMethod>();

    private bool _isConfirmingCancel;
    private bool _isChoosingMethod;
    private bool _isChangingPrice;
    private decimal? _newPrice;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public CourtDto Court { get; set; } = null!;
    [Parameter, EditorRequired] public BookingDto Booking { get; set; } = null!;
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private void StartChangingPrice()
    {
        _newPrice = Booking.Price;
        _isChangingPrice = true;
    }

    private Task PayAsync(PaymentMethod method, CancellationToken cancellationToken) =>
        RunAsync(Api.MarkPaidAsync(Booking.Id, method, cancellationToken));

    private Task CancelAsync(CancellationToken cancellationToken) =>
        RunAsync(Api.CancelBookingAsync(Booking.Id, cancellationToken));

    private Task ChangePriceAsync(CancellationToken cancellationToken) =>
        _newPrice is { } price
            ? RunAsync(Api.ChangeBookingPriceAsync(Booking.Id, price, cancellationToken))
            : Task.CompletedTask;

    private async Task RunAsync(Task<ApiResult<BookingDto>> request)
    {
        _isBusy = true;
        var result = await request;
        _isBusy = false;

        _errorMessage = result.IsSuccess ? null : result.GeneralMessage;
        if (result.IsSuccess) await OnChanged.InvokeAsync();
    }
}
