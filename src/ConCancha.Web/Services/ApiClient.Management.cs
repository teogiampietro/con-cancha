using System.Net.Http.Headers;
using ConCancha.Contracts.Cash;
using ConCancha.Contracts.Customers;
using ConCancha.Contracts.Reminders;
using ConCancha.Contracts.Reservations;

namespace ConCancha.Web.Services;

public sealed partial class ApiClient
{
    private const string ExportFileName = "clientes.csv";

    public Task<ApiResult<ReservationListDto>> GetReservationsAsync(
        ReservationRange range, ReservationFilter filter, string? search, CancellationToken cancellationToken) =>
        SendAsync<ReservationListDto>(
            HttpMethod.Get,
            $"{ApiRoutes.Reservations}?range={range}&filter={filter}&search={Uri.EscapeDataString(search ?? string.Empty)}",
            body: null, cancellationToken);

    public Task<ApiResult<IReadOnlyList<CustomerSummaryDto>>> GetCustomersAsync(
        string? search, bool onlyDebtors, bool onlyNoShows, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<CustomerSummaryDto>>(
            HttpMethod.Get, CustomerQuery(ApiRoutes.Customers, search, onlyDebtors, onlyNoShows), body: null, cancellationToken);

    public Task<ApiResult<IReadOnlyList<CustomerSuggestionDto>>> GetCustomerSuggestionsAsync(string query, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<CustomerSuggestionDto>>(
            HttpMethod.Get, $"{ApiRoutes.CustomerSuggestions}?q={Uri.EscapeDataString(query)}", body: null, cancellationToken);

    public async Task<ApiResult<FileDownload>> ExportCustomersAsync(
        string? search, bool onlyDebtors, bool onlyNoShows, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, CustomerQuery(ApiRoutes.CustomersExport, search, onlyDebtors, onlyNoShows), body: null);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return await ReadErrorAsync<FileDownload>(response, cancellationToken);

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? MediaTypeHeaderValue.Parse("text/csv").ToString();
            return ApiResult<FileDownload>.Success(new FileDownload(content, ExportFileName, contentType), (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiResult<FileDownload>.Failure(ConnectionErrorMessage, NoStatusCode);
        }
    }

    public Task<ApiResult<CashDayDto>> GetCashAsync(DateOnly date, CancellationToken cancellationToken) =>
        SendAsync<CashDayDto>(HttpMethod.Get, CashRoute(date, string.Empty), body: null, cancellationToken);

    public Task<ApiResult<CashDayDto>> CloseCashAsync(DateOnly date, CancellationToken cancellationToken) =>
        SendAsync<CashDayDto>(HttpMethod.Post, CashRoute(date, "/close"), body: null, cancellationToken);

    public Task<ApiResult<CashDayDto>> ReopenCashAsync(DateOnly date, CancellationToken cancellationToken) =>
        SendAsync<CashDayDto>(HttpMethod.Post, CashRoute(date, "/reopen"), body: null, cancellationToken);

    public Task<ApiResult<ReminderListDto>> GetRemindersAsync(ReminderDay day, CancellationToken cancellationToken) =>
        SendAsync<ReminderListDto>(HttpMethod.Get, $"{ApiRoutes.Reminders}?day={day}", body: null, cancellationToken);

    public Task<ApiResult<object>> MarkReminderSentAsync(Guid bookingId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"{ApiRoutes.Reminders}/{bookingId}/sent", body: null, cancellationToken);

    private static string CashRoute(DateOnly date, string suffix) => $"{ApiRoutes.Cash}/{date:yyyy-MM-dd}{suffix}";

    private static string CustomerQuery(string route, string? search, bool onlyDebtors, bool onlyNoShows) =>
        $"{route}?search={Uri.EscapeDataString(search ?? string.Empty)}&onlyDebtors={onlyDebtors}&onlyNoShows={onlyNoShows}";
}
