using System.Net.Http.Json;
using ConCancha.Contracts.Public;

namespace ConCancha.Tests.Support;

public static class PublicApiExtensions
{
    public const string ValidPhone = "11 2345 6789";

    private static readonly Random Random = new();

    /// <summary>A mobile number nobody else in the test run is using.</summary>
    public static string NewPhone() => "11" + Random.Next(10_000_000, 99_999_999);

    public static string PublicUrl(string slug, string path) => $"{Routes.PublicClubBase}/{slug}/{path}";

    public static string NewVisitorToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    public static Task<HttpResponseMessage> PublicBookAsync(
        this HttpClient http, string slug, Guid courtId, DateOnly date, int startMinute, CancellationToken cancellationToken,
        string? phone = null, string name = "Lucía", string? email = null, string? visitorToken = null) =>
        http.SendPublicAsync(
            HttpMethod.Post, PublicUrl(slug, "bookings"), visitorToken, cancellationToken,
            JsonContent.Create(new PublicBookingRequest(courtId, date, startMinute, name, phone ?? NewPhone(), email)));

    public static async Task<PublicBookingCreatedDto> ReadCreatedAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicBookingCreatedDto>(cancellationToken))!;
    }

    public static async Task<PublicDayDto> GetPublicDayAsync(
        this HttpClient http, string slug, DateOnly date, CancellationToken cancellationToken, string? visitorToken = null)
    {
        var response = await http.SendPublicAsync(
            HttpMethod.Get, PublicUrl(slug, $"availability?date={date:yyyy-MM-dd}"), visitorToken, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicDayDto>(cancellationToken))!;
    }

    public static Task<HttpResponseMessage> GetMyBookingsAsync(
        this HttpClient http, string slug, string? visitorToken, CancellationToken cancellationToken) =>
        http.SendPublicAsync(HttpMethod.Get, PublicUrl(slug, "my-bookings"), visitorToken, cancellationToken);

    public static Task<HttpResponseMessage> CancelMineAsync(
        this HttpClient http, string slug, Guid bookingId, string? visitorToken, CancellationToken cancellationToken) =>
        http.SendPublicAsync(HttpMethod.Post, PublicUrl(slug, $"my-bookings/{bookingId}/cancel"), visitorToken, cancellationToken);

    public static async Task<PublicMyBookingsDto> ReadMyBookingsAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicMyBookingsDto>(cancellationToken))!;
    }

    private static Task<HttpResponseMessage> SendPublicAsync(
        this HttpClient http, HttpMethod method, string url, string? visitorToken, CancellationToken cancellationToken,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (visitorToken is not null) request.Headers.Add(VisitorToken.HeaderName, visitorToken);
        return http.WithBearer(null).SendAsync(request, cancellationToken);
    }
}
