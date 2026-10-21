using ConCancha.Application.Results;
using ConCancha.Contracts.Bookings;

namespace ConCancha.Application.Abstractions;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateReservationAsync(CreateBookingRequest request, CancellationToken cancellationToken);

    Task<Result<BookingDto>> CreateBlockAsync(CreateBlockRequest request, CancellationToken cancellationToken);

    Task<Result<BookingDto>> GetAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<Result<BookingDto>> MarkPaidAsync(Guid bookingId, MarkPaidRequest request, CancellationToken cancellationToken);

    Task<Result<BookingDto>> MarkPlayedAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<Result<BookingDto>> MarkNoShowAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<Result<BookingDto>> ChangePriceAsync(Guid bookingId, ChangePriceRequest request, CancellationToken cancellationToken);

    Task<Result<BookingDto>> CancelAsync(Guid bookingId, CancellationToken cancellationToken);
}
