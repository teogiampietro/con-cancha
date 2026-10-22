using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Public;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<PublicClubDto>> GetPublicClubAsync(string slug, CancellationToken cancellationToken) =>
        SendAsync<PublicClubDto>(HttpMethod.Get, $"{ApiRoutes.PublicClubs}/{Uri.EscapeDataString(slug)}", body: null, cancellationToken);

    public Task<ApiResult<PublicDayDto>> GetPublicDayAsync(
        string slug, DateOnly date, string? visitorToken, CancellationToken cancellationToken) =>
        SendAsync<PublicDayDto>(
            HttpMethod.Get, $"{PublicBase(slug)}/availability?date={date:yyyy-MM-dd}", body: null, cancellationToken, visitorToken);

    public Task<ApiResult<PublicBookingCreatedDto>> BookPublicAsync(
        string slug, PublicBookingRequest request, string? visitorToken, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingCreatedDto>(HttpMethod.Post, $"{PublicBase(slug)}/bookings", request, cancellationToken, visitorToken);

    public Task<ApiResult<PublicBookingDto>> GetPublicBookingAsync(string slug, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingDto>(
            HttpMethod.Get, $"{PublicBase(slug)}/bookings/{Uri.EscapeDataString(token)}", body: null, cancellationToken);

    public Task<ApiResult<PublicBookingDto>> CancelPublicBookingAsync(string slug, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingDto>(
            HttpMethod.Post, $"{PublicBase(slug)}/bookings/{Uri.EscapeDataString(token)}/cancel", body: null, cancellationToken);

    public Task<ApiResult<PublicMyBookingsDto>> GetMyPublicBookingsAsync(
        string slug, string? visitorToken, CancellationToken cancellationToken) =>
        SendAsync<PublicMyBookingsDto>(HttpMethod.Get, $"{PublicBase(slug)}/my-bookings", body: null, cancellationToken, visitorToken);

    public Task<ApiResult<PublicMyBookingsDto>> CancelMyPublicBookingAsync(
        string slug, Guid bookingId, string? visitorToken, CancellationToken cancellationToken) =>
        SendAsync<PublicMyBookingsDto>(
            HttpMethod.Post, $"{PublicBase(slug)}/my-bookings/{bookingId}/cancel", body: null, cancellationToken, visitorToken);

    private static string PublicBase(string slug) => $"{ApiRoutes.PublicClubs}/{Uri.EscapeDataString(slug)}";
}
