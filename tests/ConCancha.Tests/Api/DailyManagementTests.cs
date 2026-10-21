using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Cash;
using ConCancha.Contracts.Customers;
using ConCancha.Contracts.Reminders;
using ConCancha.Contracts.Reservations;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

/// <summary>Phase 4 exit criterion: a test day is loaded, charged, and the cash register adds up with what was loaded.</summary>
public class DailyManagementTests(ApiFactory api) : ApiTestBase(api)
{
    private const int FirstSlot = CourtApiExtensions.FirstSlotMinute;
    private const int SlotLength = CourtApiExtensions.SlotMinutes;
    private const decimal Price = CourtApiExtensions.BasePrice;
    private const decimal DiscountedPrice = 15000m;

    private sealed record Club(string Token, string Slug, Guid CourtId, Guid Id);

    private static DateOnly Day => CourtApiExtensions.FutureDate(3);

    private static int Slot(int index) => FirstSlot + index * SlotLength;

    private static DateOnly ClubToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(ClubDefaults.TimeZoneId)).DateTime);

    private async Task<Club> SetUpClubAsync()
    {
        var slug = ApiClientExtensions.NewSlug();
        var owner = await Http.RegisterWithClubAsync(Ct, slug: slug);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return new Club(owner.Token, slug, court.Id, owner.Club!.Id);
    }

    private async Task<BookingDto> LoadAsync(
        Club club, DateOnly date, int slotIndex, string name = "Juan", string? phone = null,
        decimal? price = null, PaymentMethod? paidWith = null)
    {
        var request = new CreateBookingRequest(club.CourtId, date, Slot(slotIndex), name, phone, price, paidWith is not null, paidWith);
        var response = await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Bookings, request, Ct);
        return await response.ReadBookingAsync(Ct);
    }

    private async Task PostAsync(Club club, Guid bookingId, string action)
    {
        var response = await Http.WithBearer(club.Token).PostAsync($"{Routes.Bookings}/{bookingId}/{action}", null, Ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<CashDayDto> GetCashAsync(Club club, DateOnly date) =>
        (await Http.WithBearer(club.Token).GetFromJsonAsync<CashDayDto>($"{Routes.Cash}/{date:yyyy-MM-dd}", Ct))!;

    private async Task<ReservationListDto> ListReservationsAsync(
        Club club, ReservationRange range, ReservationFilter filter = ReservationFilter.All, string? search = null) =>
        (await Http.WithBearer(club.Token).GetFromJsonAsync<ReservationListDto>(
            $"{Routes.Reservations}?range={range}&filter={filter}&search={Uri.EscapeDataString(search ?? string.Empty)}", Ct))!;

    private async Task<IReadOnlyList<CustomerSummaryDto>> ListCustomersAsync(Club club, string query = "") =>
        (await Http.WithBearer(club.Token).GetFromJsonAsync<List<CustomerSummaryDto>>($"{Routes.Customers}?{query}", Ct))!;

    // ---- Caja ----

    [Fact]
    public async Task Cash_AddsUpWithWhatWasLoaded()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Paga en efectivo", paidWith: PaymentMethod.Cash);
        await LoadAsync(club, Day, 1, "Paga transferencia", price: DiscountedPrice, paidWith: PaymentMethod.Transfer);
        await LoadAsync(club, Day, 2, "Debe");
        var cancelled = await LoadAsync(club, Day, 3, "Cancela", paidWith: PaymentMethod.Cash);
        var noShow = await LoadAsync(club, Day, 4, "No vino");
        await PostAsync(club, cancelled.Id, "cancel");
        await PostAsync(club, noShow.Id, "no-show");

        var cash = await GetCashAsync(club, Day);

        Assert.Equal(Price, cash.Cash);
        Assert.Equal(DiscountedPrice, cash.Transfer);
        Assert.Equal(Price + DiscountedPrice, cash.Total);
        Assert.Equal(Price, cash.ToCollect);
        Assert.Equal(4, cash.Bookings.Count);
        Assert.DoesNotContain(cash.Bookings, line => line.CustomerName == "Cancela");
    }

    [Fact]
    public async Task Cash_ChargingATurnMovesItFromToCollectToCashIn()
    {
        var club = await SetUpClubAsync();
        var booking = await LoadAsync(club, Day, 0);

        var before = await GetCashAsync(club, Day);
        await Http.PostAsJsonAsync($"{Routes.Bookings}/{booking.Id}/pay", new MarkPaidRequest(PaymentMethod.Transfer), Ct);
        var after = await GetCashAsync(club, Day);

        Assert.Equal((0m, Price), (before.Total, before.ToCollect));
        Assert.Equal((Price, 0m), (after.Total, after.ToCollect));
        Assert.Equal(Price, after.Transfer);
    }

    [Fact]
    public async Task Cash_OtherDaysAndOtherClubsDoNotLeakIn()
    {
        var club = await SetUpClubAsync();
        var other = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, paidWith: PaymentMethod.Cash);
        await LoadAsync(other, Day, 0, paidWith: PaymentMethod.Cash);

        Assert.Equal(Price, (await GetCashAsync(club, Day)).Total);
        Assert.Equal(0m, (await GetCashAsync(club, Day.AddDays(1))).Total);
    }

    [Fact]
    public async Task Cash_ClosingOnlyReports_AndCanBeReopened()
    {
        var club = await SetUpClubAsync();
        var booking = await LoadAsync(club, Day, 0, paidWith: PaymentMethod.Cash);
        var url = $"{Routes.Cash}/{Day:yyyy-MM-dd}";

        var closed = await (await Http.PostAsync($"{url}/close", null, Ct)).Content.ReadFromJsonAsync<CashDayDto>(Ct);
        var change = await Http.PutAsJsonAsync($"{Routes.Bookings}/{booking.Id}/price", new ChangePriceRequest(1000m), Ct);
        var afterChange = await GetCashAsync(club, Day);
        var reopened = await (await Http.PostAsync($"{url}/reopen", null, Ct)).Content.ReadFromJsonAsync<CashDayDto>(Ct);

        Assert.Equal(Price, closed!.Closure!.Total);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(1000m, afterChange.Total);
        Assert.Equal(Price, afterChange.Closure!.Total);
        Assert.Null(reopened!.Closure);
    }

    [Fact]
    public async Task Cash_ClosingTwice_KeepsASingleClosure()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, paidWith: PaymentMethod.Cash);
        var url = $"{Routes.Cash}/{Day:yyyy-MM-dd}/close";

        await Http.PostAsync(url, null, Ct);
        var second = await Http.PostAsync(url, null, Ct);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    // ---- Indicadores de la agenda ----

    [Fact]
    public async Task AgendaStats_OccupancyCollectedAndStillOwed()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, paidWith: PaymentMethod.Cash);
        await LoadAsync(club, Day, 1);
        await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Blocks, new CreateBlockRequest(club.CourtId, Day, Slot(2), "Lluvia"), Ct);

        var stats = (await Http.GetAgendaAsync(club.Token, Day, Ct)).Stats;

        Assert.Equal(3, stats.OccupiedSlots);
        Assert.Equal((int)Math.Round(3 * 100.0 / stats.TotalSlots), stats.OccupancyPercent);
        Assert.Equal(Price, stats.Collected);
        Assert.Equal(Price, stats.ToCollect);
        Assert.Equal(0m, stats.Missed);
    }

    [Fact]
    public async Task AgendaStats_MissedIsThePriceOfFreeSlotsThatAlreadyPassed()
    {
        var club = await SetUpClubAsync();
        var past = CourtApiExtensions.FutureDate(-3);
        await LoadAsync(club, past, 0);

        var stats = (await Http.GetAgendaAsync(club.Token, past, Ct)).Stats;

        Assert.Equal((stats.TotalSlots - 1) * Price, stats.Missed);
    }

    [Fact]
    public async Task AgendaStats_WithoutCourts_AreZero()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var stats = (await Http.GetAgendaAsync(owner.Token, Day, Ct)).Stats;

        Assert.Equal(new Contracts.Agenda.AgendaStatsDto(0, 0, 0, 0m, 0m, 0m), stats);
    }

    // ---- Reservas ----

    [Fact]
    public async Task Reservations_FilterByPeriod()
    {
        var club = await SetUpClubAsync();
        var today = ClubToday();
        await LoadAsync(club, today, 0, "De hoy");
        await LoadAsync(club, today.AddDays(3), 0, "En tres días");
        await LoadAsync(club, today.AddDays(20), 0, "En veinte días");
        await LoadAsync(club, today.AddDays(-3), 0, "De hace tres días");

        string[] Names(ReservationListDto list) => list.Items.Select(i => i.Booking.CustomerName!).ToArray();

        Assert.Equal(["De hoy"], Names(await ListReservationsAsync(club, ReservationRange.Today)));
        Assert.Equal(["De hoy", "En tres días"], Names(await ListReservationsAsync(club, ReservationRange.Next7Days)));
        Assert.Equal(["De hoy", "En tres días", "En veinte días"], Names(await ListReservationsAsync(club, ReservationRange.Next30Days)));
        Assert.Equal(["De hace tres días"], Names(await ListReservationsAsync(club, ReservationRange.History)));
    }

    [Fact]
    public async Task Reservations_HistoryShowsTheMostRecentFirst()
    {
        var club = await SetUpClubAsync();
        var today = ClubToday();
        await LoadAsync(club, today.AddDays(-5), 0, "Más viejo");
        await LoadAsync(club, today.AddDays(-2), 0, "Más nuevo");

        var list = await ListReservationsAsync(club, ReservationRange.History);

        Assert.Equal(["Más nuevo", "Más viejo"], list.Items.Select(i => i.Booking.CustomerName));
    }

    [Fact]
    public async Task Reservations_ToCollectOnlyListsWhatIsStillOwed()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Pagó", paidWith: PaymentMethod.Cash);
        await LoadAsync(club, Day, 1, "Debe");
        var noShow = await LoadAsync(club, Day, 2, "No vino");
        await PostAsync(club, noShow.Id, "no-show");

        var list = await ListReservationsAsync(club, ReservationRange.Next7Days, ReservationFilter.ToCollect);

        Assert.Equal(["Debe"], list.Items.Select(i => i.Booking.CustomerName));
    }

    [Fact]
    public async Task Reservations_OnlineFilterListsPublicBookingsOnly_AndBlocksNeverAppear()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Del mostrador");
        await Http.PublicBookAsync(club.Slug, club.CourtId, Day, Slot(1), Ct, name: "Reservó online");
        await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Blocks, new CreateBlockRequest(club.CourtId, Day, Slot(2), "Lluvia"), Ct);

        var online = await ListReservationsAsync(club, ReservationRange.Next7Days, ReservationFilter.Online);
        var all = await ListReservationsAsync(club, ReservationRange.Next7Days);

        Assert.Equal(["Reservó online"], online.Items.Select(i => i.Booking.CustomerName));
        Assert.Equal(2, all.Items.Count);
        Assert.All(all.Items, item => Assert.Equal("Pádel 1", item.CourtName));
    }

    [Fact]
    public async Task Reservations_CancelledOnesStayInTheListWithTheirStatus()
    {
        var club = await SetUpClubAsync();
        var booking = await LoadAsync(club, Day, 0, "Cancelada");
        await PostAsync(club, booking.Id, "cancel");

        var list = await ListReservationsAsync(club, ReservationRange.Next7Days);

        Assert.Equal(BookingStatus.Cancelled, Assert.Single(list.Items).Booking.Status);
    }

    [Theory]
    [InlineData("mar", "María Gómez")]
    [InlineData("GÓMEZ", "María Gómez")]
    [InlineData("5555", "María Gómez")]
    [InlineData("011 15 5555 1234", null)]
    public async Task Reservations_SearchByNameOrPhone(string search, string? expectedName)
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "María Gómez", phone: "11 5555 1234");
        await LoadAsync(club, Day, 1, "Pedro López", phone: "11 4444 9999");

        var found = (await ListReservationsAsync(club, ReservationRange.Next7Days, search: search)).Items
            .Select(i => i.Booking.CustomerName).ToList();

        if (expectedName is not null) Assert.Equal([expectedName], found);
        else Assert.Equal(["María Gómez"], found);
    }

    [Fact]
    public async Task Reservations_EachClubOnlySeesItsOwn()
    {
        var club = await SetUpClubAsync();
        var other = await SetUpClubAsync();
        await LoadAsync(other, Day, 0, "De otro club");

        Assert.Empty((await ListReservationsAsync(club, ReservationRange.Next7Days)).Items);
    }

    // ---- Clientes ----

    [Fact]
    public async Task Customers_SummarizeVisitsSpentDebtAndNoShows()
    {
        var club = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        await LoadAsync(club, Day, 0, "Carla", phone, paidWith: PaymentMethod.Cash);
        await LoadAsync(club, Day, 1, "Carla", phone);
        var missed = await LoadAsync(club, Day, 2, "Carla", phone);
        var cancelled = await LoadAsync(club, Day, 3, "Carla", phone, paidWith: PaymentMethod.Cash);
        await PostAsync(club, missed.Id, "no-show");
        await PostAsync(club, cancelled.Id, "cancel");

        var carla = Assert.Single(await ListCustomersAsync(club));

        Assert.Equal("Carla", carla.Name);
        Assert.Equal((2, Price, Price, 1), (carla.Visits, carla.Spent, carla.Debt, carla.NoShows));
        Assert.StartsWith("549", carla.WhatsAppPhone);
    }

    [Fact]
    public async Task Customers_SamePhoneWrittenDifferently_IsOneCustomer()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Luis", "11 2345 6789");
        await LoadAsync(club, Day, 1, "Luis", "+54 9 11 2345-6789");

        Assert.Equal(2, Assert.Single(await ListCustomersAsync(club)).Visits);
    }

    [Fact]
    public async Task Customers_PeopleLoadedWithoutPhoneAreGroupedByName()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Sin Teléfono");
        await LoadAsync(club, Day, 1, "sin teléfono");

        var customer = Assert.Single(await ListCustomersAsync(club));

        Assert.Equal(2, customer.Visits);
        Assert.Null(customer.Id);
    }

    [Fact]
    public async Task Customers_FiltersAndSearch()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Al día", PublicApiExtensions.NewPhone(), paidWith: PaymentMethod.Cash);
        await LoadAsync(club, Day, 1, "Deudor", "11 5555 0001");
        var missed = await LoadAsync(club, Day, 2, "Faltador", PublicApiExtensions.NewPhone());
        await PostAsync(club, missed.Id, "no-show");

        Assert.Equal(["Deudor"], (await ListCustomersAsync(club, "onlyDebtors=true")).Select(c => c.Name));
        Assert.Equal(["Faltador"], (await ListCustomersAsync(club, "onlyNoShows=true")).Select(c => c.Name));
        Assert.Equal(["Deudor"], (await ListCustomersAsync(club, "search=5555")).Select(c => c.Name));
        Assert.Equal(["Al día"], (await ListCustomersAsync(club, "search=al%20d")).Select(c => c.Name));
        Assert.Equal(["Al día", "Deudor", "Faltador"], (await ListCustomersAsync(club)).Select(c => c.Name));
    }

    [Fact]
    public async Task Customers_EachClubOnlySeesItsOwn()
    {
        var club = await SetUpClubAsync();
        var other = await SetUpClubAsync();
        await LoadAsync(other, Day, 0, "De otro club", PublicApiExtensions.NewPhone());

        Assert.Empty(await ListCustomersAsync(club));
    }

    [Fact]
    public async Task Customers_ExportIsACsvThatOpensInExcel()
    {
        var club = await SetUpClubAsync();
        await LoadAsync(club, Day, 0, "Exportada", PublicApiExtensions.NewPhone());

        var response = await Http.WithBearer(club.Token).GetAsync(Routes.CustomersExport, Ct);
        var text = await response.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Cliente;WhatsApp", text);
        Assert.Contains("Exportada", text);
    }

    // ---- Recordatorios ----

    [Fact]
    public async Task Reminders_ListTomorrowsTurnsThatHaveAPhone()
    {
        var club = await SetUpClubAsync();
        var tomorrow = ClubToday().AddDays(1);
        await LoadAsync(club, tomorrow, 0, "Con teléfono", "11 2345 6789");
        await LoadAsync(club, tomorrow, 1, "Sin teléfono");
        var cancelled = await LoadAsync(club, tomorrow, 2, "Cancelado", "11 2345 0000");
        await PostAsync(club, cancelled.Id, "cancel");
        await LoadAsync(club, tomorrow.AddDays(1), 0, "Pasado mañana", "11 2345 1111");

        var list = await Http.WithBearer(club.Token).GetFromJsonAsync<ReminderListDto>($"{Routes.Reminders}?day=Tomorrow", Ct);

        var reminder = Assert.Single(list!.Reminders);
        Assert.Equal("Con teléfono", reminder.CustomerName);
        Assert.Equal("5491123456789", reminder.WhatsAppPhone);
        Assert.Equal(tomorrow, reminder.Date);
        Assert.False(reminder.IsSent);
        Assert.False(string.IsNullOrEmpty(list.ClubName));
    }

    [Fact]
    public async Task Reminders_TodayLeavesOutTurnsAlreadyPlayedOrMissed()
    {
        var club = await SetUpClubAsync();
        var today = ClubToday();
        await LoadAsync(club, today, 0, "Pendiente", "11 2345 6789");
        var played = await LoadAsync(club, today, 1, "Jugó", "11 2345 0001");
        await PostAsync(club, played.Id, "played");

        var list = await Http.WithBearer(club.Token).GetFromJsonAsync<ReminderListDto>($"{Routes.Reminders}?day=Today", Ct);

        Assert.Equal(["Pendiente"], list!.Reminders.Select(r => r.CustomerName));
    }

    [Fact]
    public async Task Reminders_MarkingAsSentIsRemembered()
    {
        var club = await SetUpClubAsync();
        var booking = await LoadAsync(club, ClubToday().AddDays(1), 0, "Ana", "11 2345 6789");

        var response = await Http.PostAsync($"{Routes.Reminders}/{booking.Id}/sent", null, Ct);
        var list = await Http.GetFromJsonAsync<ReminderListDto>($"{Routes.Reminders}?day=Tomorrow", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(Assert.Single(list!.Reminders).IsSent);
    }

    [Fact]
    public async Task Reminders_CannotMarkATurnOfAnotherClub()
    {
        var club = await SetUpClubAsync();
        var other = await SetUpClubAsync();
        var booking = await LoadAsync(other, Day, 0, "Ana", "11 2345 6789");

        var response = await Http.WithBearer(club.Token).PostAsync($"{Routes.Reminders}/{booking.Id}/sent", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Permisos ----

    [Fact]
    public async Task DailyManagementScreens_AreOpenToStaff_ButNotToAnonymous()
    {
        var club = await SetUpClubAsync();
        var staff = await CreateStaffSessionAsync(club.Id);
        var urls = new[] { Routes.Reservations, Routes.Customers, $"{Routes.Cash}/{Day:yyyy-MM-dd}", Routes.Reminders };

        foreach (var url in urls)
        {
            Assert.Equal(HttpStatusCode.OK, (await Http.WithBearer(staff.Token).GetAsync(url, Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Http.WithBearer(null).GetAsync(url, Ct)).StatusCode);
        }
    }
}
