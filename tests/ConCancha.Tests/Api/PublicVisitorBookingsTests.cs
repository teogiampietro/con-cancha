using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using ConCancha.Api.Controllers;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Public;
using ConCancha.Domain.Bookings;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.RateLimiting;

namespace ConCancha.Tests.Api;

/// <summary>M6: a browser sees and cancels its own bookings from the club's page, with no account and never by phone.</summary>
public class PublicVisitorBookingsTests(ApiFactory api) : ApiTestBase(api)
{
    private const int FirstSlot = CourtApiExtensions.FirstSlotMinute;
    private const int SlotLength = CourtApiExtensions.SlotMinutes;
    private const int ShortVisitorTokenLength = 8;

    private sealed record Club(string Token, string Slug, Guid CourtId);

    private static DateOnly Soon => CourtApiExtensions.FutureDate(3);

    private async Task<Club> SetUpClubAsync()
    {
        var slug = ApiClientExtensions.NewSlug();
        var owner = await Http.RegisterWithClubAsync(Ct, slug: slug);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return new Club(owner.Token, slug, court.Id);
    }

    [Fact]
    public async Task MyBookings_ListsTheBookingsMadeWithTheSameVisitorToken()
    {
        var club = await SetUpClubAsync();
        var visitor = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: visitor);

        var mine = await (await Http.GetMyBookingsAsync(club.Slug, visitor, Ct)).ReadMyBookingsAsync(Ct);

        var booking = Assert.Single(mine.Bookings);
        Assert.Equal(FirstSlot, booking.StartMinuteOfDay);
        Assert.True(booking.CanCancel);
    }

    [Fact]
    public async Task MyBookings_AnotherVisitorWithTheSamePhoneSeesNothing()
    {
        var club = await SetUpClubAsync();
        var phone = PublicApiExtensions.NewPhone();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, phone, visitorToken: PublicApiExtensions.NewVisitorToken());

        var other = await (await Http.GetMyBookingsAsync(club.Slug, PublicApiExtensions.NewVisitorToken(), Ct)).ReadMyBookingsAsync(Ct);

        Assert.Empty(other.Bookings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("corto")]
    [InlineData("no-es-alfanumerico-no-es-alfanumerico-no-es-alfanumerico")]
    public async Task MyBookings_WithoutAValidToken_IsEmpty(string? token)
    {
        var club = await SetUpClubAsync();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: PublicApiExtensions.NewVisitorToken());

        var mine = await (await Http.GetMyBookingsAsync(club.Slug, token, Ct)).ReadMyBookingsAsync(Ct);

        Assert.Empty(mine.Bookings);
    }

    [Fact]
    public async Task MyBookings_BookingWithoutAValidToken_IsNotListedForAnyone()
    {
        var club = await SetUpClubAsync();
        var shortToken = new string('a', ShortVisitorTokenLength);
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: shortToken);

        var mine = await (await Http.GetMyBookingsAsync(club.Slug, shortToken, Ct)).ReadMyBookingsAsync(Ct);

        Assert.Empty(mine.Bookings);
    }

    [Fact]
    public async Task MyBookings_AreScopedToTheClub()
    {
        var clubA = await SetUpClubAsync();
        var clubB = await SetUpClubAsync();
        var visitor = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(clubA.Slug, clubA.CourtId, Soon, FirstSlot, Ct, visitorToken: visitor);

        var inOtherClub = await (await Http.GetMyBookingsAsync(clubB.Slug, visitor, Ct)).ReadMyBookingsAsync(Ct);

        Assert.Empty(inOtherClub.Bookings);
    }

    [Fact]
    public async Task MyBookings_UnknownClub_IsNotFound()
    {
        var response = await Http.GetMyBookingsAsync("no-existe-nunca", PublicApiExtensions.NewVisitorToken(), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelMine_FreesTheSlotAndLeavesTheList()
    {
        var club = await SetUpClubAsync();
        var visitor = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: visitor);
        var booking = (await (await Http.GetMyBookingsAsync(club.Slug, visitor, Ct)).ReadMyBookingsAsync(Ct)).Bookings.Single();

        var response = await Http.CancelMineAsync(club.Slug, booking.Id, visitor, Ct);
        var day = await Http.GetPublicDayAsync(club.Slug, Soon, Ct);

        Assert.Empty((await response.ReadMyBookingsAsync(Ct)).Bookings);
        Assert.Equal(PublicSlotState.Free, day.Courts.Single().Slots[0].State);
    }

    [Fact]
    public async Task CancelMine_BookingOfAnotherVisitor_IsNotFoundAndStaysBooked()
    {
        var club = await SetUpClubAsync();
        var owner = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: owner);
        var booking = (await (await Http.GetMyBookingsAsync(club.Slug, owner, Ct)).ReadMyBookingsAsync(Ct)).Bookings.Single();

        var response = await Http.CancelMineAsync(club.Slug, booking.Id, PublicApiExtensions.NewVisitorToken(), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single((await (await Http.GetMyBookingsAsync(club.Slug, owner, Ct)).ReadMyBookingsAsync(Ct)).Bookings);
    }

    [Fact]
    public async Task CancelMine_InsideTheCancellationWindow_IsRejected()
    {
        var club = await SetUpClubAsync();
        var visitor = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: visitor);
        await Http.WithBearer(club.Token).PutAsJsonAsync(Routes.ClubCancellation, new UpdateCancellationRequest(BookingPolicy.MaxCancellationWindowHours), Ct);
        var booking = (await (await Http.GetMyBookingsAsync(club.Slug, visitor, Ct)).ReadMyBookingsAsync(Ct)).Bookings.Single();

        var response = await Http.CancelMineAsync(club.Slug, booking.Id, visitor, Ct);

        Assert.False(booking.CanCancel);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Availability_MarksTheVisitorsOwnSlotAsMine_AndOthersAsTaken()
    {
        var club = await SetUpClubAsync();
        var visitor = PublicApiExtensions.NewVisitorToken();
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot, Ct, visitorToken: visitor);
        await Http.PublicBookAsync(club.Slug, club.CourtId, Soon, FirstSlot + SlotLength, Ct, visitorToken: PublicApiExtensions.NewVisitorToken());

        var asVisitor = (await Http.GetPublicDayAsync(club.Slug, Soon, Ct, visitor)).Courts.Single().Slots;
        var asStranger = (await Http.GetPublicDayAsync(club.Slug, Soon, Ct)).Courts.Single().Slots;

        Assert.Equal([PublicSlotState.Mine, PublicSlotState.Taken], asVisitor.Take(2).Select(slot => slot.State));
        Assert.Equal([PublicSlotState.Taken, PublicSlotState.Taken], asStranger.Take(2).Select(slot => slot.State));
    }

    [Fact]
    public async Task Availability_ABlockIsNotAvailable_NotTaken()
    {
        var club = await SetUpClubAsync();
        await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Blocks, new CreateBlockRequest(club.CourtId, Soon, FirstSlot, "Lluvia"), Ct);

        var slots = (await Http.GetPublicDayAsync(club.Slug, Soon, Ct)).Courts.Single().Slots;
        var raw = await Http.GetStringAsync(PublicApiExtensions.PublicUrl(club.Slug, $"availability?date={Soon:yyyy-MM-dd}"), Ct);

        Assert.Equal(PublicSlotState.Blocked, slots[0].State);
        Assert.DoesNotContain("Lluvia", raw);
    }

    [Theory]
    [InlineData(nameof(PublicBookingsController.GetMyBookings))]
    [InlineData(nameof(PublicBookingsController.CancelMine))]
    public void NewEndpoints_AreRateLimited(string action)
    {
        var method = typeof(PublicBookingsController).GetMethod(action)!;

        Assert.NotNull(method.GetCustomAttribute<EnableRateLimitingAttribute>());
    }
}
