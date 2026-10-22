using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<AgendaDayDto>> GetAgendaAsync(DateOnly date, CancellationToken cancellationToken) =>
        SendAsync<AgendaDayDto>(HttpMethod.Get, $"{ApiRoutes.Agenda}?date={date:yyyy-MM-dd}", body: null, cancellationToken);

    public Task<ApiResult<BookingDto>> CreateReservationAsync(CreateBookingRequest request, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, ApiRoutes.Bookings, request, cancellationToken);

    public Task<ApiResult<BookingDto>> CreateBlockAsync(CreateBlockRequest request, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, ApiRoutes.Blocks, request, cancellationToken);

    public Task<ApiResult<BookingDto>> MarkPaidAsync(Guid bookingId, PaymentMethod method, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/pay", new MarkPaidRequest(method), cancellationToken);

    public Task<ApiResult<BookingDto>> MarkPlayedAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/played", body: null, cancellationToken);

    public Task<ApiResult<BookingDto>> MarkNoShowAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/no-show", body: null, cancellationToken);

    public Task<ApiResult<BookingDto>> ChangeBookingPriceAsync(Guid bookingId, decimal price, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Put, $"{ApiRoutes.Bookings}/{bookingId}/price", new ChangePriceRequest(price), cancellationToken);

    public Task<ApiResult<BookingDto>> MoveBookingAsync(Guid bookingId, MoveBookingRequest request, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/move", request, cancellationToken);

    public Task<ApiResult<BookingDto>> MarkUnpaidAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/unpay", body: null, cancellationToken);

    public Task<ApiResult<BookingDto>> ResetAttendanceAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/attendance/reset", body: null, cancellationToken);

    public Task<ApiResult<BookingDto>> ChangePaymentMethodAsync(Guid bookingId, PaymentMethod method, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Put, $"{ApiRoutes.Bookings}/{bookingId}/payment-method", new MarkPaidRequest(method), cancellationToken);

    public Task<ApiResult<BookingDto>> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<BookingDto>(HttpMethod.Post, $"{ApiRoutes.Bookings}/{bookingId}/cancel", body: null, cancellationToken);
}
