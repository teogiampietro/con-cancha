using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Public;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<PublicClubDto>> GetPublicClubAsync(string slug, CancellationToken cancellationToken) =>
        SendAsync<PublicClubDto>(HttpMethod.Get, $"{ApiRoutes.PublicClubs}/{Uri.EscapeDataString(slug)}", body: null, cancellationToken);

    public Task<ApiResult<PublicDayDto>> GetPublicDayAsync(string slug, DateOnly date, CancellationToken cancellationToken) =>
        SendAsync<PublicDayDto>(
            HttpMethod.Get, $"{PublicBase(slug)}/availability?date={date:yyyy-MM-dd}", body: null, cancellationToken);

    public Task<ApiResult<PublicBookingCreatedDto>> BookPublicAsync(
        string slug, PublicBookingRequest request, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingCreatedDto>(HttpMethod.Post, $"{PublicBase(slug)}/bookings", request, cancellationToken);

    public Task<ApiResult<PublicBookingDto>> GetPublicBookingAsync(string slug, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingDto>(
            HttpMethod.Get, $"{PublicBase(slug)}/bookings/{Uri.EscapeDataString(token)}", body: null, cancellationToken);

    public Task<ApiResult<PublicBookingDto>> CancelPublicBookingAsync(string slug, string token, CancellationToken cancellationToken) =>
        SendAsync<PublicBookingDto>(
            HttpMethod.Post, $"{PublicBase(slug)}/bookings/{Uri.EscapeDataString(token)}/cancel", body: null, cancellationToken);

    private static string PublicBase(string slug) => $"{ApiRoutes.PublicClubs}/{Uri.EscapeDataString(slug)}";
}
