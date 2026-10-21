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

    public static Task<HttpResponseMessage> PublicBookAsync(
        this HttpClient http, string slug, Guid courtId, DateOnly date, int startMinute, CancellationToken cancellationToken,
        string? phone = null, string name = "Lucía", string? email = null) =>
        http.WithBearer(null).PostAsJsonAsync(
            PublicUrl(slug, "bookings"),
            new PublicBookingRequest(courtId, date, startMinute, name, phone ?? NewPhone(), email),
            cancellationToken);

    public static async Task<PublicBookingCreatedDto> ReadCreatedAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicBookingCreatedDto>(cancellationToken))!;
    }

    public static async Task<PublicDayDto> GetPublicDayAsync(
        this HttpClient http, string slug, DateOnly date, CancellationToken cancellationToken) =>
        (await http.WithBearer(null).GetFromJsonAsync<PublicDayDto>(PublicUrl(slug, $"availability?date={date:yyyy-MM-dd}"), cancellationToken))!;
}
