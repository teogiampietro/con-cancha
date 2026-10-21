using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Pricing;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

public class CourtsAndAgendaTests(ApiFactory api) : ApiTestBase(api)
{
    private const int Hour = 60;
    private const int EndOfDay = 24 * Hour;

    [Fact]
    public async Task Courts_EachClubOnlySeesItsOwn()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        var courtA = await Http.CreateCourtAsync(a.Token, Ct);

        var listA = await Http.WithBearer(a.Token).GetFromJsonAsync<List<CourtDto>>(Routes.Courts, Ct);
        var listB = await Http.WithBearer(b.Token).GetFromJsonAsync<List<CourtDto>>(Routes.Courts, Ct);

        Assert.Equal([courtA.Id], listA!.Select(c => c.Id));
        Assert.Empty(listB!);
    }

    [Fact]
    public async Task Courts_PricesOfAnotherClubCannotBeReadOrChanged()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        var courtA = await Http.CreateCourtAsync(a.Token, Ct);
        var rule = await Http.CreateBasePriceAsync(a.Token, courtA.Id, Ct);

        Http.WithBearer(b.Token);

        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync($"{Routes.Courts}/{courtA.Id}/price-rules", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.DeleteAsync($"{Routes.PriceRules}/{rule.Id}", Ct)).StatusCode);
    }

    [Fact]
    public async Task Courts_StaffCanReadButNotChange()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);
        Http.WithBearer(staff.Token);

        var list = await Http.GetAsync(Routes.Courts, Ct);
        var create = await Http.PostAsJsonAsync(Routes.Courts, new CourtRequest("Cancha", Sport.Padel, 90, null, null), Ct);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Theory]
    [InlineData(45, null, null)]
    [InlineData(90, 600, null)]
    [InlineData(90, 1200, 600)]
    [InlineData(90, 0, 1500)]
    public async Task CreateCourt_InvalidInput_IsRejected(int slotMinutes, int? opening, int? closing)
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token)
            .PostAsJsonAsync(Routes.Courts, new CourtRequest("Cancha", Sport.Padel, slotMinutes, opening, closing), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourt_DuplicateName_IsAConflict()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.CreateCourtAsync(owner.Token, Ct, "Pádel 1");

        var response = await Http.PostAsJsonAsync(Routes.Courts, new CourtRequest("Pádel 1", Sport.Padel, 90, null, null), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Agenda_GeneratesSlotsFromClubHours()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.CreateCourtAsync(owner.Token, Ct);

        var court = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(), Ct)).Courts.Single();

        Assert.Equal(8 * Hour, court.Slots[0].StartMinuteOfDay);
        Assert.All(court.Slots, slot => Assert.True(slot.EndMinuteOfDay <= EndOfDay));
        Assert.All(court.Slots, slot => Assert.Null(slot.Booking));
    }

    [Fact]
    public async Task Agenda_CourtHoursOverrideTheClubHours()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.Courts, new CourtRequest("Cancha corta", Sport.Padel, 60, 10 * Hour, 14 * Hour), Ct);

        var court = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(), Ct)).Courts.Single();

        Assert.Equal([10 * Hour, 11 * Hour, 12 * Hour, 13 * Hour], court.Slots.Select(s => s.StartMinuteOfDay));
    }

    [Fact]
    public async Task Agenda_PricesFollowTheRulesByDayAndTime()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct, slotMinutes: 60);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct, price: 100m);
        var night = new PriceRuleRequest(null, 18 * Hour, EndOfDay, 150m);
        var weekend = new PriceRuleRequest(Weekdays.Saturday | Weekdays.Sunday, 0, EndOfDay, 200m);
        await Http.PostAsJsonAsync($"{Routes.Courts}/{court.Id}/price-rules", night, Ct);
        await Http.PostAsJsonAsync($"{Routes.Courts}/{court.Id}/price-rules", weekend, Ct);

        var monday = NextDay(DayOfWeek.Monday);
        var saturday = NextDay(DayOfWeek.Saturday);
        var mondaySlots = (await Http.GetAgendaAsync(owner.Token, monday, Ct)).Courts.Single().Slots;
        var saturdaySlots = (await Http.GetAgendaAsync(owner.Token, saturday, Ct)).Courts.Single().Slots;

        Assert.Equal(100m, mondaySlots.Single(s => s.StartMinuteOfDay == 10 * Hour).Price);
        Assert.Equal(150m, mondaySlots.Single(s => s.StartMinuteOfDay == 20 * Hour).Price);
        Assert.Equal(200m, saturdaySlots.Single(s => s.StartMinuteOfDay == 20 * Hour).Price);
    }

    [Fact]
    public async Task Agenda_ShowsBookingsOnTheirSlot_AndDropsCancelledOnes()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        var date = CourtApiExtensions.FutureDate();
        var booking = await (await Http.BookAsync(owner.Token, court.Id, date, CourtApiExtensions.FirstSlotMinute, Ct)).ReadBookingAsync(Ct);

        var before = (await Http.GetAgendaAsync(owner.Token, date, Ct)).Courts.Single().Slots[0];
        await Http.PostAsync($"{Routes.Bookings}/{booking.Id}/cancel", null, Ct);
        var after = (await Http.GetAgendaAsync(owner.Token, date, Ct)).Courts.Single().Slots[0];

        Assert.Equal(booking.Id, before.Booking!.Id);
        Assert.Null(after.Booking);
    }

    [Fact]
    public async Task Agenda_PastDaysAreMarkedAsPast()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.CreateCourtAsync(owner.Token, Ct);

        var past = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(-3), Ct)).Courts.Single().Slots;
        var future = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(3), Ct)).Courts.Single().Slots;

        Assert.All(past, slot => Assert.True(slot.IsPast));
        Assert.All(future, slot => Assert.False(slot.IsPast));
    }

    [Fact]
    public async Task Agenda_EachClubOnlySeesItsOwnCourts()
    {
        var a = await Http.RegisterWithClubAsync(Ct);
        var b = await Http.RegisterWithClubAsync(Ct);
        await Http.CreateCourtAsync(a.Token, Ct);

        Assert.Single((await Http.GetAgendaAsync(a.Token, CourtApiExtensions.FutureDate(), Ct)).Courts);
        Assert.Empty((await Http.GetAgendaAsync(b.Token, CourtApiExtensions.FutureDate(), Ct)).Courts);
    }

    [Fact]
    public async Task UpdateClubHours_ChangesTheGrid()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.CreateCourtAsync(owner.Token, Ct, slotMinutes: 60);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubHours, new UpdateClubHoursRequest(10 * Hour, 12 * Hour), Ct);
        var slots = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(), Ct)).Courts.Single().Slots;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([10 * Hour, 11 * Hour], slots.Select(s => s.StartMinuteOfDay));
    }

    private static DateOnly NextDay(DayOfWeek day)
    {
        var date = CourtApiExtensions.FutureDate(7);
        while (date.DayOfWeek != day) date = date.AddDays(1);
        return date;
    }
}
