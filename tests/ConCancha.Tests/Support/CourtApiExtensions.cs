using System.Net.Http.Json;
using ConCancha.Contracts.Agenda;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Domain.Courts;

namespace ConCancha.Tests.Support;

public static class CourtApiExtensions
{
    public const int SlotMinutes = 90;
    public const int FirstSlotMinute = 8 * 60;
    public const decimal BasePrice = 20000m;

    /// <summary>A date far enough ahead that no slot is in the past.</summary>
    public static DateOnly FutureDate(int daysAhead = 30) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAhead));

    public static async Task<CourtDto> CreateCourtAsync(
        this HttpClient http, string token, CancellationToken cancellationToken, string name = "Pádel 1", int slotMinutes = SlotMinutes)
    {
        var request = new CourtRequest(name, Sport.Padel, slotMinutes, null, null);
        var response = await http.WithBearer(token).PostAsJsonAsync(Routes.Courts, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourtDto>(cancellationToken))!;
    }

    public static async Task<PriceRuleDto> CreateBasePriceAsync(
        this HttpClient http, string token, Guid courtId, CancellationToken cancellationToken, decimal price = BasePrice)
    {
        var response = await http.WithBearer(token)
            .PostAsJsonAsync($"{Routes.Courts}/{courtId}/price-rules", new PriceRuleRequest(null, 0, 24 * 60, price), cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PriceRuleDto>(cancellationToken))!;
    }

    public static Task<HttpResponseMessage> BookAsync(
        this HttpClient http, string token, Guid courtId, DateOnly date, int startMinute, CancellationToken cancellationToken,
        decimal? price = null, string name = "Juan") =>
        http.WithBearer(token).PostAsJsonAsync(
            Routes.Bookings, new CreateBookingRequest(courtId, date, startMinute, name, null, price, false, null), cancellationToken);

    public static async Task<BookingDto> ReadBookingAsync(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(cancellationToken))!;
    }

    public static async Task<AgendaDayDto> GetAgendaAsync(
        this HttpClient http, string token, DateOnly date, CancellationToken cancellationToken) =>
        (await http.WithBearer(token).GetFromJsonAsync<AgendaDayDto>($"{Routes.Agenda}?date={date:yyyy-MM-dd}", cancellationToken))!;
}
