using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Bookings;
using ConCancha.Domain.Bookings;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

public class BookingsTests(ApiFactory api) : ApiTestBase(api)
{
    private const int ParallelRequests = 12;
    private const int SecondSlotMinute = CourtApiExtensions.FirstSlotMinute + CourtApiExtensions.SlotMinutes;

    private async Task<(string Token, Guid CourtId, Guid ClubId)> SetUpClubWithCourtAsync()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return (owner.Token, court.Id, owner.Club!.Id);
    }

    [Fact]
    public async Task CreateReservation_UsesTheCourtPriceWhenNoneIsGiven()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();

        var booking = await (await Http.BookAsync(token, courtId, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct))
            .ReadBookingAsync(Ct);

        Assert.Equal(CourtApiExtensions.BasePrice, booking.Price);
        Assert.Equal(PaymentStatus.Pending, booking.PaymentStatus);
        Assert.Equal(CourtApiExtensions.FirstSlotMinute + CourtApiExtensions.SlotMinutes, booking.EndMinuteOfDay);
    }

    [Fact]
    public async Task CreateReservation_PriceCanBeOverriddenWhenLoading()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();

        var booking = await (await Http.BookAsync(token, courtId, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct, price: 15000m))
            .ReadBookingAsync(Ct);

        Assert.Equal(15000m, booking.Price);
    }

    [Fact]
    public async Task CreateReservation_SlotThatIsNotOnTheGrid_IsRejected()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();

        var response = await Http.BookAsync(token, courtId, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute + 15, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_WithoutAnyPrice_AsksForOne()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);

        var response = await Http.BookAsync(owner.Token, court.Id, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_SameSlotTwice_SecondGetsConflict()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var date = CourtApiExtensions.FutureDate();

        var first = await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct);
        var second = await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_ConcurrentRequestsForTheSameSlot_LeaveASingleBooking()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var date = CourtApiExtensions.FutureDate();

        var responses = await Task.WhenAll(Enumerable.Range(0, ParallelRequests)
            .Select(i => Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct, name: $"Cliente {i}")));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(ParallelRequests - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var slot = (await Http.GetAgendaAsync(token, date, Ct)).Courts.Single().Slots
            .Single(s => s.StartMinuteOfDay == CourtApiExtensions.FirstSlotMinute);
        Assert.NotNull(slot.Booking);
    }

    [Fact]
    public async Task CreateReservation_AfterCancelling_TheSlotCanBeBookedAgain()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var date = CourtApiExtensions.FutureDate();
        var booking = await (await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct)).ReadBookingAsync(Ct);

        var cancel = await Http.PostAsync($"{Routes.Bookings}/{booking.Id}/cancel", null, Ct);
        var again = await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_OccupiesTheSlotAndCannotBePaid()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var date = CourtApiExtensions.FutureDate();

        var block = await (await Http.WithBearer(token).PostAsJsonAsync(
            Routes.Blocks, new CreateBlockRequest(courtId, date, CourtApiExtensions.FirstSlotMinute, "Lluvia"), Ct)).ReadBookingAsync(Ct);
        var overlap = await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct);
        var pay = await Http.PostAsJsonAsync($"{Routes.Bookings}/{block.Id}/pay", new MarkPaidRequest(PaymentMethod.Cash), Ct);

        Assert.Equal(BookingKind.Block, block.Kind);
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, pay.StatusCode);
    }

    [Fact]
    public async Task Booking_PayPlayAndNoShowFollowTheRules()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var date = CourtApiExtensions.FutureDate();
        var first = await (await Http.BookAsync(token, courtId, date, CourtApiExtensions.FirstSlotMinute, Ct)).ReadBookingAsync(Ct);
        var second = await (await Http.BookAsync(token, courtId, date, SecondSlotMinute, Ct)).ReadBookingAsync(Ct);

        var paid = await (await Http.PostAsJsonAsync($"{Routes.Bookings}/{first.Id}/pay", new MarkPaidRequest(PaymentMethod.Transfer), Ct)).ReadBookingAsync(Ct);
        var paidAgain = await Http.PostAsJsonAsync($"{Routes.Bookings}/{first.Id}/pay", new MarkPaidRequest(PaymentMethod.Cash), Ct);
        var paidNoShow = await Http.PostAsync($"{Routes.Bookings}/{first.Id}/no-show", null, Ct);
        var played = await (await Http.PostAsync($"{Routes.Bookings}/{first.Id}/played", null, Ct)).ReadBookingAsync(Ct);
        var noShow = await (await Http.PostAsync($"{Routes.Bookings}/{second.Id}/no-show", null, Ct)).ReadBookingAsync(Ct);

        Assert.Equal((PaymentStatus.Paid, PaymentMethod.Transfer), (paid.PaymentStatus, paid.PaymentMethod));
        Assert.Equal(HttpStatusCode.Conflict, paidAgain.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, paidNoShow.StatusCode);
        Assert.Equal(Attendance.Played, played.Attendance);
        Assert.Equal(Attendance.NoShow, noShow.Attendance);
    }

    [Fact]
    public async Task Booking_PriceCanBeChangedFromTheDetail_ButNotWhenCancelled()
    {
        var (token, courtId, _) = await SetUpClubWithCourtAsync();
        var booking = await (await Http.BookAsync(token, courtId, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct)).ReadBookingAsync(Ct);

        var changed = await (await Http.PutAsJsonAsync($"{Routes.Bookings}/{booking.Id}/price", new ChangePriceRequest(18000m), Ct)).ReadBookingAsync(Ct);
        await Http.PostAsync($"{Routes.Bookings}/{booking.Id}/cancel", null, Ct);
        var afterCancel = await Http.PutAsJsonAsync($"{Routes.Bookings}/{booking.Id}/price", new ChangePriceRequest(1m), Ct);

        Assert.Equal(18000m, changed.Price);
        Assert.Equal(HttpStatusCode.Conflict, afterCancel.StatusCode);
    }

    [Fact]
    public async Task Booking_OfAnotherClub_IsNotFound()
    {
        var (tokenA, courtA, _) = await SetUpClubWithCourtAsync();
        var (tokenB, _, _) = await SetUpClubWithCourtAsync();
        var booking = await (await Http.BookAsync(tokenA, courtA, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct)).ReadBookingAsync(Ct);

        Http.WithBearer(tokenB);

        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync($"{Routes.Bookings}/{booking.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.PostAsync($"{Routes.Bookings}/{booking.Id}/cancel", null, Ct)).StatusCode);
    }

    [Fact]
    public async Task Booking_CannotUseACourtOfAnotherClub()
    {
        var (_, courtA, _) = await SetUpClubWithCourtAsync();
        var (tokenB, _, _) = await SetUpClubWithCourtAsync();

        var response = await Http.BookAsync(tokenB, courtA, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Staff_CanLoadBookings()
    {
        var (_, courtId, clubId) = await SetUpClubWithCourtAsync();
        var staff = await CreateStaffSessionAsync(clubId);

        var response = await Http.BookAsync(staff.Token, courtId, CourtApiExtensions.FutureDate(), CourtApiExtensions.FirstSlotMinute, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Bookings_WithoutToken_Return401()
    {
        Http.WithBearer(null);

        var response = await Http.PostAsJsonAsync(
            Routes.Bookings, new CreateBookingRequest(Guid.NewGuid(), CourtApiExtensions.FutureDate(), 0, "Ana", null, null, false, null), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
