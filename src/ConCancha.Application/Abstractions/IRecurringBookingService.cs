using ConCancha.Application.Results;
using ConCancha.Contracts.RecurringBookings;

namespace ConCancha.Application.Abstractions;

public interface IRecurringBookingService
{
    Task<Result<IReadOnlyList<RecurringBookingDto>>> ListByCourtAsync(Guid courtId, CancellationToken cancellationToken);

    Task<Result<RecurringBookingResultDto>> CreateAsync(Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken);

    Task<Result<RecurringBookingResultDto>> UpdateAsync(
        Guid courtId, Guid recurringId, UpdateRecurringBookingRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid courtId, Guid recurringId, CancellationToken cancellationToken);
}
