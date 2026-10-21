using ConCancha.Contracts.Reservations;

namespace ConCancha.Application.Abstractions;

public interface IReservationService
{
    Task<ReservationListDto> ListAsync(
        ReservationRange range, ReservationFilter filter, string? search, CancellationToken cancellationToken);
}
