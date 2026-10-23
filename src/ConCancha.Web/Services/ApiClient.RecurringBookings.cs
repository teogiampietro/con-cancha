using ConCancha.Contracts.RecurringBookings;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    private static string RecurringBookingsUrl(Guid courtId) =>
        $"{ApiRoutes.Courts}/{courtId}/{ApiRoutes.RecurringBookingsSegment}";

    public Task<ApiResult<IReadOnlyList<RecurringBookingDto>>> GetRecurringBookingsAsync(Guid courtId, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<RecurringBookingDto>>(HttpMethod.Get, RecurringBookingsUrl(courtId), body: null, cancellationToken);

    public Task<ApiResult<RecurringBookingResultDto>> CreateRecurringBookingAsync(
        Guid courtId, RecurringBookingRequest request, CancellationToken cancellationToken) =>
        SendAsync<RecurringBookingResultDto>(HttpMethod.Post, RecurringBookingsUrl(courtId), request, cancellationToken);

    public Task<ApiResult<RecurringBookingResultDto>> UpdateRecurringBookingAsync(
        Guid courtId, Guid recurringId, UpdateRecurringBookingRequest request, CancellationToken cancellationToken) =>
        SendAsync<RecurringBookingResultDto>(HttpMethod.Put, $"{RecurringBookingsUrl(courtId)}/{recurringId}", request, cancellationToken);

    public Task<ApiResult<object>> DeleteRecurringBookingAsync(Guid courtId, Guid recurringId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, $"{RecurringBookingsUrl(courtId)}/{recurringId}", body: null, cancellationToken);
}
