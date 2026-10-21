using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Public;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

/// <summary>Phase 3 exit criterion: a player books and cancels without an account, and the turn shows up in the club's agenda.</summary>
public class PublicBookingsTests(ApiFactory api) : ApiTestBase(api)
{
    private const int ParallelRequests = 12;
    private const int FirstSlot = CourtApiExtensions.FirstSlotMinute;
    private const int SlotLength = CourtApiExtensions.SlotMinutes;
    private const int BeyondTheWindowHours = 1000;

    private sealed record Club(string Token, string Slug, Guid CourtId, Guid Id);

    private async Task<Club> SetUpClubAsync(bool withPrice = true)
    {
        var slug = ApiClientExtensions.NewSlug();
        var owner = await Http.RegisterWithClubAsync(Ct, slug: slug);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        if (withPrice) await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return new Club(owner.Token, slug, court.Id, owner.Club!.Id);
    }

    private static DateOnly Soon => CourtApiExtensions.FutureDate(3);

    [Fact]
    public async Task Availability_ListsSlotsWithPricesAndNoCustomerData()
    {
        var club = await SetUpClubAsync();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, name: "Nombre Secreto");

        var court = (await Http.GetPublicDayAsync(club.Slug, Soon, Ct)).Courts.Single();
        var raw = await Http.GetStringAsync(PublicApiExtensions.PublicUrl(club.Slug, $"availability?date={Soon:yyyy-MM-dd}"), Ct);

        Assert.Equal(PublicSlotState.Taken, court.Slots[0].State);
        Assert.Equal(PublicSlotState.Free, court.Slots[1].State);
        Assert.Equal(CourtApiExtensions.BasePrice, court.Slots[1].Price);
        Assert.DoesNotContain("Nombre Secreto", raw);
    }

    [Fact]
    public async Task Availability_UnknownClub_IsNotFound()
    {
        var response = await Http.WithBearer(null).GetAsync(PublicApiExtensions.PublicUrl("no-existe-nunca", $"availability?date={Soon:yyyy-MM-dd}"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Availability_OnlyOffersTheNextSevenDays()
    {
        var club = await SetUpClubAsync();
        var far = await Http.WithBearer(null).GetAsync(PublicApiExtensions.PublicUrl(club.Slug, $"availability?date={CourtApiExtensions.FutureDate(9):yyyy-MM-dd}"), Ct);
        var past = await Http.WithBearer(null).GetAsync(PublicApiExtensions.PublicUrl(club.Slug, $"availability?date={CourtApiExtensions.FutureDate(-3):yyyy-MM-dd}"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, far.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
    }

    [Fact]
    public async Task Book_WithoutAccount_CreatesTheTurnAndItAppearsInTheClubAgenda()
    {
        var club = await SetUpClubAsync();

        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, name: "Marcos")).ReadCreatedAsync(Ct);
        var slot = (await Http.GetAgendaAsync(club.Token, Soon, Ct)).Courts.Single().Slots[0];

        Assert.False(string.IsNullOrEmpty(created.ManagementToken));
        Assert.Equal(CourtApiExtensions.BasePrice, created.Booking.Price);
        Assert.True(created.Booking.CanCancel);
        Assert.Equal("Marcos", slot.Booking!.CustomerName);
        Assert.Equal(BookingOrigin.Online, slot.Booking.Origin);
        Assert.Equal(PaymentStatus.Pending, slot.Booking.PaymentStatus);
    }

    [Fact]
    public async Task Book_StoresOnlyTheHashOfTheManagementToken()
    {
        var club = await SetUpClubAsync();

        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);

        using var scope = Api.Services.CreateScope();
        var hashes = await scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>()
            .Bookings.IgnoreQueryFilters().Select(b => b.ManagementTokenHash).ToListAsync(Ct);
        Assert.DoesNotContain(created.ManagementToken, hashes);
        Assert.Contains(hashes, hash => hash is { Length: 64 });
    }

    [Fact]
    public async Task Book_SameSlotTwice_SecondGetsConflict()
    {
        var club = await SetUpClubAsync();

        var first = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);
        var second = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Book_ConcurrentPlayersForTheSameSlot_OnlyOneWins()
    {
        var club = await SetUpClubAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, ParallelRequests)
            .Select(_ => Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(ParallelRequests - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Book_ThirdActiveTurnForTheSamePhone_IsRejected_EvenIfWrittenDifferently()
    {
        var club = await SetUpClubAsync();
        var one = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone: "11 2345 6789");
        var two = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + SlotLength, Ct, phone: "+54 9 11 2345-6789");

        var three = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + 2 * SlotLength, Ct, phone: "011 15 2345 6789");

        Assert.Equal(HttpStatusCode.Created, one.StatusCode);
        Assert.Equal(HttpStatusCode.Created, two.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, three.StatusCode);
    }

    [Fact]
    public async Task Book_AfterCancellingOne_ThePhoneCanBookAgain()
    {
        var club = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        var first = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone)).ReadCreatedAsync(Ct);
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + SlotLength, Ct, phone);

        await Http.PostAsync(PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{first.ManagementToken}/cancel"), null, Ct);
        var again = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + 2 * SlotLength, Ct, phone);

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Book_TheLimitIsPerClub()
    {
        var clubA = await SetUpClubAsync();
        var clubB = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        await Http.PublicBookAsync(clubA.Slug, clubA.CourtId, Soon, FirstSlot, Ct, phone);
        await Http.PublicBookAsync(clubA.Slug, clubA.CourtId, Soon, FirstSlot + SlotLength, Ct, phone);

        var otherClub = await Http.PublicBookAsync(clubB.Slug, clubB.CourtId, Soon, FirstSlot, Ct, phone);

        Assert.Equal(HttpStatusCode.Created, otherClub.StatusCode);
    }

    [Fact]
    public async Task Book_SamePhoneTwice_IsASingleCustomer()
    {
        var club = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone, email: "ana@prueba.test");
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + SlotLength, Ct, "+54 9 " + phone);

        using var scope = Api.Services.CreateScope();
        var customers = await scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>()
            .Customers.IgnoreQueryFilters().Where(c => c.ClubId == club.Id).ToListAsync(Ct);

        Assert.Equal("ana@prueba.test", Assert.Single(customers).Email);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("no es un teléfono")]
    [InlineData("+1 415 555 2671")]
    public async Task Book_InvalidPhone_IsRejected(string phone)
    {
        var club = await SetUpClubAsync();

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_InvalidEmailOrMissingName_IsRejected()
    {
        var club = await SetUpClubAsync();

        var badEmail = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, email: "esto-no-es-un-mail");
        var noName = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, name: "");

        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);
    }

    [Fact]
    public async Task Book_OutsideTheOfferedDays_IsRejected()
    {
        var club = await SetUpClubAsync();

        var far = await Http.PublicBookAsync(club.Slug, club.CourtId, CourtApiExtensions.FutureDate(9), FirstSlot, Ct);
        var past = await Http.PublicBookAsync(club.Slug, club.CourtId, CourtApiExtensions.FutureDate(-3), FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, far.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
    }

    [Fact]
    public async Task Book_SlotThatIsNotOnTheGrid_IsRejected()
    {
        var club = await SetUpClubAsync();

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + 15, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_CourtWithoutPrice_AsksToCallTheClub()
    {
        var club = await SetUpClubAsync(withPrice: false);

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Book_CourtOfAnotherClub_IsNotFound()
    {
        var clubA = await SetUpClubAsync();
        var clubB = await SetUpClubAsync();

        var response = await Http.PublicBookAsync(clubB.Slug, clubA.CourtId, Soon, FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Book_InactiveCourt_IsRejected()
    {
        var club = await SetUpClubAsync();
        await Http.WithBearer(club.Token).PutAsJsonAsync(
            $"{Routes.Courts}/{club.CourtId}",
            new CourtRequest("Pádel 1", Sport.Padel, CourtApiExtensions.SlotMinutes, null, null, IsActive: false), Ct);

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_SlotBlockedByTheClub_IsTaken()
    {
        var club = await SetUpClubAsync();
        await Http.WithBearer(club.Token).PostAsJsonAsync(
            Routes.Blocks, new CreateBlockRequest(club.CourtId, Soon, FirstSlot, "Lluvia"), Ct);

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetBooking_WithTheLink_ShowsTheTurn()
    {
        var club = await SetUpClubAsync();
        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);

        var booking = await Http.GetFromJsonAsync<PublicBookingDto>(PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{created.ManagementToken}"), Ct);

        Assert.Equal(created.Booking, booking);
        Assert.Equal("Pádel 1", booking!.CourtName);
        Assert.Equal(FirstSlot, booking.StartMinuteOfDay);
    }

    [Fact]
    public async Task GetBooking_WrongTokenOrOtherClub_IsNotFound()
    {
        var clubA = await SetUpClubAsync();
        var clubB = await SetUpClubAsync();
        var created = await (await Http.PublicBookAsync(clubA.Slug, clubA.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);

        var wrong = await Http.GetAsync(PublicApiExtensions.PublicUrl(clubA.Slug, "bookings/token-inventado"), Ct);
        var otherClub = await Http.GetAsync(PublicApiExtensions.PublicUrl(clubB.Slug, $"bookings/{created.ManagementToken}"), Ct);
        var cancelOtherClub = await Http.PostAsync(PublicApiExtensions.PublicUrl(clubB.Slug, $"bookings/{created.ManagementToken}/cancel"), null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherClub.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancelOtherClub.StatusCode);
    }

    [Fact]
    public async Task Cancel_OutsideTheWindow_FreesTheSlotInTheAgenda()
    {
        var club = await SetUpClubAsync();
        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);

        var response = await Http.PostAsync(PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{created.ManagementToken}/cancel"), null, Ct);
        var cancelled = (await response.Content.ReadFromJsonAsync<PublicBookingDto>(Ct))!;
        var slot = (await Http.GetAgendaAsync(club.Token, Soon, Ct)).Courts.Single().Slots[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.False(cancelled.CanCancel);
        Assert.Null(slot.Booking);
    }

    [Fact]
    public async Task Cancel_Twice_SecondIsAConflict()
    {
        var club = await SetUpClubAsync();
        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);
        var url = PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{created.ManagementToken}/cancel");

        await Http.PostAsync(url, null, Ct);

        Assert.Equal(HttpStatusCode.Conflict, (await Http.PostAsync(url, null, Ct)).StatusCode);
    }

    [Fact]
    public async Task Cancel_InsideTheWindow_IsRefusedAndTheTurnStays()
    {
        var club = await SetUpClubAsync();
        await SetCancellationWindowAsync(club.Id, BeyondTheWindowHours);
        var created = await (await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct)).ReadCreatedAsync(Ct);

        var response = await Http.PostAsync(PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{created.ManagementToken}/cancel"), null, Ct);
        var booking = await Http.GetFromJsonAsync<PublicBookingDto>(PublicApiExtensions.PublicUrl(club.Slug, $"bookings/{created.ManagementToken}"), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(created.Booking.CanCancel);
        Assert.Equal(BookingStatus.Confirmed, booking!.Status);
    }

    [Fact]
    public async Task PanelCancellation_IsNotLimitedByTheWindow()
    {
        var club = await SetUpClubAsync();
        await SetCancellationWindowAsync(club.Id, BeyondTheWindowHours);
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct);
        var bookingId = (await Http.GetAgendaAsync(club.Token, Soon, Ct)).Courts.Single().Slots[0].Booking!.Id;

        var response = await Http.WithBearer(club.Token).PostAsync($"{Routes.Bookings}/{bookingId}/cancel", null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PanelBooking_WithPhone_CountsTowardsThePublicLimit()
    {
        var club = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        foreach (var start in new[] { FirstSlot, FirstSlot + SlotLength })
        {
            await Http.WithBearer(club.Token).PostAsJsonAsync(
                Routes.Bookings,
                new CreateBookingRequest(club.CourtId, Soon, start, "Cargado en mostrador", phone, null, false, null), Ct);
        }

        var response = await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + 2 * SlotLength, Ct, phone);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task SetCancellationWindowAsync(Guid clubId, int hours)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        await db.Clubs.Where(c => c.Id == clubId).ExecuteUpdateAsync(set => set.SetProperty(c => c.CancellationWindowHours, hours), Ct);
    }

    [Fact]
    public async Task PublicEndpoints_AreRateLimitedPerIp()
    {
        const int allowedPerMinute = 2;
        var club = await SetUpClubAsync();
        using var limited = Api.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:PublicBookingPerMinute"] = allowedPerMinute.ToString() })));
        var http = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt <= allowedPerMinute; attempt++)
            statuses.Add((await http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone: "123")).StatusCode);

        Assert.Equal([HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.TooManyRequests], statuses);
    }
}
