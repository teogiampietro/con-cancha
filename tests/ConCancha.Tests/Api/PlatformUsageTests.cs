using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Platform;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

/// <summary>Exact figures for the overview and the club detail, on seeded turns with known days.</summary>
[Collection(ApiCollection.Name)]
public class PlatformUsageTests(ApiFactory api) : ApiTestBase(api)
{
    private const int DaysInPeriod = 7;
    private static DateOnly Today => PlatformTestData.LocalToday();
    private static DateOnly PeriodFrom => Today.AddDays(1 - DaysInPeriod);

    [Fact]
    public async Task OverviewCountsCountedTurnsOfThePeriodAgainstThePreviousOne()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var court = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        var inactiveCourt = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 2", isActive: false);
        var customer = await PlatformTestData.SeedCustomerAsync(Api, club, "Ana", "5491111111111");
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-1), 20), customer);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-2), 10), origin: BookingOrigin.Online);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-3), 9), attendance: Attendance.NoShow);
        await PlatformTestData.SeedTurnAsync(Api, club, inactiveCourt, PlatformTestData.Local(Today.AddDays(-3), 20), status: BookingStatus.Cancelled);
        await PlatformTestData.SeedTurnAsync(Api, club, inactiveCourt, PlatformTestData.Local(Today.AddDays(-4), 20), kind: BookingKind.Block, customer: null);
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-9), 20), customer);

        var overview = await GetOverviewAsync(PeriodFrom, Today);

        var row = overview.Clubs.Single(clubRow => clubRow.Id == club.Id);
        Assert.Equal(2, row.BookingsInPeriod);
        Assert.Equal(1, row.BookingsInPreviousPeriod);
        Assert.Equal(1, row.OnlineBookingsInPeriod);
        Assert.Equal(0.3, row.AverageBookingsPerDay);
        Assert.Equal(1, row.ActiveCourts);
        Assert.Equal(1, row.Customers);
        Assert.Equal(ClubActivity.Active, row.Activity);
    }

    [Fact]
    public async Task OverviewTotalsAddUpTheClubs()
    {
        var overview = await GetOverviewAsync(PeriodFrom, Today);

        Assert.Equal(overview.Clubs.Sum(club => club.BookingsInPeriod), overview.Totals.BookingsInPeriod);
        Assert.Equal(overview.Clubs.Sum(club => club.BookingsInPreviousPeriod), overview.Totals.BookingsInPreviousPeriod);
        Assert.Equal(overview.Clubs.Count, overview.Totals.Clubs);
    }

    [Fact]
    public async Task ClubCreatedTodayWithoutTurnsIsNewAndOneWithOldTurnsOnlyIsInactive()
    {
        var fresh = await PlatformTestData.SeedClubAsync(Api);
        var stale = await PlatformTestData.SeedClubAsync(Api, createdAt: DateTimeOffset.UtcNow.AddDays(-30));
        var staleCourt = await PlatformTestData.SeedCourtAsync(Api, stale, "Cancha 1");
        await PlatformTestData.SeedTurnAsync(Api, stale, staleCourt, PlatformTestData.Local(Today.AddDays(-20), 20));

        var overview = await GetOverviewAsync(PeriodFrom, Today);

        Assert.Equal(ClubActivity.New, overview.Clubs.Single(club => club.Id == fresh.Id).Activity);
        Assert.Equal(ClubActivity.Inactive, overview.Clubs.Single(club => club.Id == stale.Id).Activity);
        Assert.Null(overview.Clubs.Single(club => club.Id == fresh.Id).LastBookingOn);
    }

    [Fact]
    public async Task TurnLateAtNightCountsOnItsLocalDay()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var court = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today, 23, 30));
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(PeriodFrom.AddDays(-1), 23, 30));

        var detail = await GetDetailAsync(club.Id, PeriodFrom, Today);

        Assert.Equal(1, detail.Usage.BookingsInPeriod);
        Assert.Equal(1, detail.Usage.BookingsInPreviousPeriod);
        Assert.Equal(1, detail.Series.Single(day => day.Start == Today).Bookings);
    }

    [Fact]
    public async Task DetailListsCourtsActiveFirstWithTheirTurns()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var active = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        var inactive = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 2", isActive: false);
        await PlatformTestData.SeedTurnAsync(Api, club, active, PlatformTestData.Local(Today.AddDays(-2), 20));
        await PlatformTestData.SeedTurnAsync(Api, club, active, PlatformTestData.Local(Today.AddDays(-1), 20));

        var detail = await GetDetailAsync(club.Id, PeriodFrom, Today);

        Assert.Collection(detail.Courts,
            court => { Assert.Equal("Cancha 1", court.Name); Assert.True(court.IsActive); Assert.Equal(2, court.BookingsInPeriod); },
            court => { Assert.Equal("Cancha 2", court.Name); Assert.False(court.IsActive); Assert.Equal(0, court.BookingsInPeriod); });
        Assert.Equal(UsageGranularity.Day, detail.Granularity);
        Assert.Equal(DaysInPeriod, detail.Series.Count);
        Assert.Equal(2, detail.Series.Sum(day => day.Bookings));
    }

    [Fact]
    public async Task DetailGroupsByWeekWhenThePeriodIsLongerThanAMonth()
    {
        var club = await PlatformTestData.SeedClubAsync(Api);
        var court = await PlatformTestData.SeedCourtAsync(Api, club, "Cancha 1");
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-30), 20));
        await PlatformTestData.SeedTurnAsync(Api, club, court, PlatformTestData.Local(Today.AddDays(-45), 20));

        var detail = await GetDetailAsync(club.Id, Today.AddDays(-59), Today);

        Assert.Equal(UsageGranularity.Week, detail.Granularity);
        Assert.All(detail.Series, week => Assert.Equal(DayOfWeek.Monday, week.Start.DayOfWeek));
        Assert.Equal(detail.Usage.BookingsInPeriod, detail.Series.Sum(week => week.Bookings));
        Assert.Equal(2, detail.Usage.BookingsInPeriod);
    }

    [Fact]
    public async Task WithoutDatesThePanelShowsTheCurrentWeek()
    {
        var overview = await GetOverviewAsync(from: null, to: null);

        Assert.Equal(DayOfWeek.Monday, overview.Period.From.DayOfWeek);
        Assert.Equal(overview.Period.From.AddDays(6), overview.Period.To);
        Assert.True(overview.Period.From <= Today && Today <= overview.Period.To);
        Assert.Equal(overview.Period.From.AddDays(-1), overview.PreviousPeriod.To);
    }

    [Theory]
    [InlineData("from=2026-10-10")]
    [InlineData("from=2026-10-10&to=2026-10-01")]
    [InlineData("from=2025-01-01&to=2026-12-31")]
    [InlineData("from=2026-13-40&to=2026-10-01")]
    public async Task InvalidPeriodIsRejected(string query)
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);

        var response = await Http.WithBearer(token).GetAsync($"api/platform/overview?{query}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownClubDetailIsNotFound()
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);

        var response = await Http.WithBearer(token).GetAsync($"api/platform/clubs/{Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PlatformOverviewDto> GetOverviewAsync(DateOnly? from, DateOnly? to)
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);
        var query = from is null || to is null ? string.Empty : $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await Http.WithBearer(token).GetAsync($"api/platform/overview{query}", Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlatformOverviewDto>(Ct))!;
    }

    private async Task<ClubUsageDetailDto> GetDetailAsync(Guid clubId, DateOnly from, DateOnly to)
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Http, Ct);
        var response = await Http.WithBearer(token).GetAsync($"api/platform/clubs/{clubId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClubUsageDetailDto>(Ct))!;
    }
}
