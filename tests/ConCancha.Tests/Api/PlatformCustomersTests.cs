using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

[Collection(ApiCollection.Name)]
public class PlatformCustomersTests(ApiFactory api) : ApiTestBase(api)
{
    private const int PageSize = 50;
    private static DateOnly Today => PlatformTestData.LocalToday();

    [Fact]
    public async Task ListsCustomersLatestTurnFirstAndCustomersWithoutTurnsLast()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var court = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        var recent = await PlatformTestData.SeedCustomerAsync(Api, club, "Ana", "5491111111111");
        var older = await PlatformTestData.SeedCustomerAsync(Api, club, "Beto", "5491122222222");
        var none = await PlatformTestData.SeedCustomerAsync(Api, club, "Carla", "5491133333333");
        await PlatformTestData.SeedTurnAsync(Api, club, court, DateTimeOffset.UtcNow.AddDays(-2), recent);
        await PlatformTestData.SeedTurnAsync(Api, club, court, DateTimeOffset.UtcNow.AddDays(-10), older);

        var page = await GetCustomersAsync(club.Id, search: null, page: 1);

        Assert.Equal(new[] { "Ana", "Beto", "Carla" }, page.Items.Select(customer => customer.Name));
        Assert.Equal(3, page.Total);
        Assert.Null(page.Items[2].LastBookingOn);
    }

    [Fact]
    public async Task CountsOnlyCountedTurnsInThePeriodAndKeepsTheLastOneOverAllTime()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var court = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        var customer = await PlatformTestData.SeedCustomerAsync(Api, club, "Ana", "5491111111111");
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-2), 20), customer);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-20), 20), customer);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-1), 20), customer, attendance: Attendance.NoShow);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-1), 18), customer, status: BookingStatus.Cancelled);

        var page = await GetCustomersAsync(club.Id, search: null, page: 1, from: Today.AddDays(-5), to: Today);

        var row = Assert.Single(page.Items);
        Assert.Equal(1, row.BookingsInPeriod);
        Assert.Equal(Today.AddDays(-2), row.LastBookingOn);
    }

    [Fact]
    public async Task SearchesByNameAndByPhoneWithoutMatchingOtherClubs()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var other = await PlatformTestData.SeedClubAsync(Api);
        await PlatformTestData.SeedCustomerAsync(Api, club, "Ana Pérez", "5491111111111");
        await PlatformTestData.SeedCustomerAsync(Api, club, "Beto", "5491122223333");
        await PlatformTestData.SeedCustomerAsync(Api, other, "Ana Otra", "5491111111111");

        var byName = await GetCustomersAsync(club.Id, search: "ana", page: 1);
        var byPhone = await GetCustomersAsync(club.Id, search: "2223333", page: 1);

        Assert.Equal("Ana Pérez", Assert.Single(byName.Items).Name);
        Assert.Equal("Beto", Assert.Single(byPhone.Items).Name);
    }

    [Fact]
    public async Task PagesFiftyCustomersAtATime()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        for (var index = 0; index < PageSize + 5; index++)
            await PlatformTestData.SeedCustomerAsync(Api, club, $"Cliente {index:D2}", $"54911{index:D8}");

        var first = await GetCustomersAsync(club.Id, search: null, page: 1);
        var second = await GetCustomersAsync(club.Id, search: null, page: 2);

        Assert.Equal(PageSize, first.Items.Count);
        Assert.Equal(5, second.Items.Count);
        Assert.Equal(PageSize + 5, second.Total);
    }

    [Fact]
    public async Task UnknownClubIsNotFoundAndPageZeroIsRejected()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);

        var missing = await Http.WithBearer(token).GetAsync($"api/platform/clubs/{Guid.NewGuid()}/customers", Ct);
        var zero = await Http.WithBearer(token).GetAsync($"api/platform/clubs/{club.Id}/customers?page=0", Ct);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    private async Task<PlatformCustomersPageDto> GetCustomersAsync(
        Guid clubId, string? search, int page, DateOnly? from = null, DateOnly? to = null)
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);
        var query = new List<string> { $"page={page}" };
        if (search is not null) query.Add($"search={Uri.EscapeDataString(search)}");
        if (from is { } start) query.Add($"from={start:yyyy-MM-dd}");
        if (to is { } end) query.Add($"to={end:yyyy-MM-dd}");

        var response = await Http.WithBearer(token).GetAsync($"api/platform/clubs/{clubId}/customers?{string.Join('&', query)}", Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlatformCustomersPageDto>(Ct))!;
    }
}
