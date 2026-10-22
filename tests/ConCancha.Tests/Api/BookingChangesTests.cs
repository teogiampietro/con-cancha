using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Cash;
using ConCancha.Contracts.Customers;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

/// <summary>Module M2: moving a turn, undoing its state and payment, and the customer suggestions.</summary>
public class BookingChangesTests(ApiFactory api) : ApiTestBase(api)
{
    private const int FirstSlot = CourtApiExtensions.FirstSlotMinute;
    private const int SecondSlot = FirstSlot + CourtApiExtensions.SlotMinutes;
    private const int ParallelMoves = 8;
    private const string SecondCourtName = "Pádel 2";

    private sealed record Club(string Token, Guid ClubId, Guid CourtId);

    private static DateOnly Day => CourtApiExtensions.FutureDate();

    private async Task<Club> SetUpClubAsync()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return new Club(owner.Token, owner.Club!.Id, court.Id);
    }

    private async Task<BookingDto> BookAsync(Club club, int startMinute, string name = "Juan", PaymentMethod? paidWith = null)
    {
        var request = new CreateBookingRequest(club.CourtId, Day, startMinute, name, null, null, paidWith is not null, paidWith);
        return await (await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Bookings, request, Ct)).ReadBookingAsync(Ct);
    }

    private Task<HttpResponseMessage> MoveAsync(string token, Guid bookingId, Guid courtId, int startMinute) =>
        Http.WithBearer(token).PostAsJsonAsync($"{Routes.Bookings}/{bookingId}/move", new MoveBookingRequest(courtId, startMinute), Ct);

    private Task<HttpResponseMessage> PostAsync(string token, Guid bookingId, string action) =>
        Http.WithBearer(token).PostAsync($"{Routes.Bookings}/{bookingId}/{action}", null, Ct);

    private async Task<CourtDto> AddCourtAsync(Club club, string name, int slotMinutes)
    {
        var court = await Http.CreateCourtAsync(club.Token, Ct, name, slotMinutes);
        await Http.CreateBasePriceAsync(club.Token, court.Id, Ct);
        return court;
    }

    private async Task<CashDayDto> GetCashAsync(Club club) =>
        (await Http.WithBearer(club.Token).GetFromJsonAsync<CashDayDto>($"{Routes.Cash}/{Day:yyyy-MM-dd}", Ct))!;

    [Fact]
    public async Task Move_ToAFreeSlotOfAnotherCourt_KeepsTheDayAndFreesTheOldSlot()
    {
        var club = await SetUpClubAsync();
        var other = await AddCourtAsync(club, SecondCourtName, CourtApiExtensions.SlotMinutes);
        var booking = await BookAsync(club, FirstSlot);

        var moved = await (await MoveAsync(club.Token, booking.Id, other.Id, SecondSlot)).ReadBookingAsync(Ct);
        var again = await BookAsync(club, FirstSlot, "Ana");

        Assert.Equal((other.Id, SecondSlot, Day), (moved.CourtId, moved.StartMinuteOfDay, moved.Date));
        Assert.Equal(FirstSlot, again.StartMinuteOfDay);
    }

    [Fact]
    public async Task Move_ToAnOccupiedSlot_IsAConflict()
    {
        var club = await SetUpClubAsync();
        var booking = await BookAsync(club, FirstSlot);
        await BookAsync(club, SecondSlot, "Ana");

        var response = await MoveAsync(club.Token, booking.Id, club.CourtId, SecondSlot);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Move_ToACourtWithAnotherDuration_IsRejected()
    {
        var club = await SetUpClubAsync();
        var shortCourt = await AddCourtAsync(club, SecondCourtName, slotMinutes: 60);
        var booking = await BookAsync(club, FirstSlot);

        var response = await MoveAsync(club.Token, booking.Id, shortCourt.Id, FirstSlot);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Move_ToTheSameSlot_OrOffTheGrid_IsRejected()
    {
        var club = await SetUpClubAsync();
        var booking = await BookAsync(club, FirstSlot);

        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(club.Token, booking.Id, club.CourtId, FirstSlot)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await MoveAsync(club.Token, booking.Id, club.CourtId, FirstSlot + 15)).StatusCode);
    }

    [Fact]
    public async Task Move_CancelledTurnOrBlock_IsRejected()
    {
        var club = await SetUpClubAsync();
        var cancelled = await BookAsync(club, FirstSlot);
        await PostAsync(club.Token, cancelled.Id, "cancel");
        var block = await (await Http.WithBearer(club.Token).PostAsJsonAsync(
            Routes.Blocks, new CreateBlockRequest(club.CourtId, Day, SecondSlot, "Lluvia"), Ct)).ReadBookingAsync(Ct);
        var target = FirstSlot + 2 * CourtApiExtensions.SlotMinutes;

        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(club.Token, cancelled.Id, club.CourtId, target)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(club.Token, block.Id, club.CourtId, target)).StatusCode);
    }

    [Fact]
    public async Task Move_ToAPastSlot_IsRejected()
    {
        var club = await SetUpClubAsync();
        var request = new CreateBookingRequest(club.CourtId, CourtApiExtensions.FutureDate(-3), FirstSlot, "Juan", null, null, false, null);
        var booking = await (await Http.WithBearer(club.Token).PostAsJsonAsync(Routes.Bookings, request, Ct)).ReadBookingAsync(Ct);

        var response = await MoveAsync(club.Token, booking.Id, club.CourtId, SecondSlot);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Move_ConcurrentMovesToTheSameSlot_LeaveASingleWinner()
    {
        var club = await SetUpClubAsync();
        var target = FirstSlot + (ParallelMoves + 1) * CourtApiExtensions.SlotMinutes;
        var bookings = await Task.WhenAll(Enumerable.Range(0, ParallelMoves)
            .Select(index => BookAsync(club, FirstSlot + index * CourtApiExtensions.SlotMinutes, $"Cliente {index}")));

        var responses = await Task.WhenAll(bookings.Select(booking => MoveAsync(club.Token, booking.Id, club.CourtId, target)));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(ParallelMoves - 1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Move_OfAnotherClub_IsNotFound_AndCannotUseItsCourt()
    {
        var a = await SetUpClubAsync();
        var b = await SetUpClubAsync();
        var booking = await BookAsync(a, FirstSlot);
        var ownBooking = await BookAsync(b, FirstSlot);

        Assert.Equal(HttpStatusCode.NotFound, (await MoveAsync(b.Token, booking.Id, b.CourtId, SecondSlot)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await MoveAsync(b.Token, ownBooking.Id, a.CourtId, SecondSlot)).StatusCode);
    }

    [Fact]
    public async Task Unpay_ReturnsTheTurnToPending_AndTheCashRegisterFollows()
    {
        var club = await SetUpClubAsync();
        var booking = await BookAsync(club, FirstSlot, paidWith: PaymentMethod.Cash);
        var before = await GetCashAsync(club);

        var unpaid = await (await PostAsync(club.Token, booking.Id, "unpay")).ReadBookingAsync(Ct);
        var after = await GetCashAsync(club);

        Assert.Equal((PaymentStatus.Pending, null), (unpaid.PaymentStatus, unpaid.PaymentMethod));
        Assert.Equal((CourtApiExtensions.BasePrice, 0m), (before.Cash, after.Cash));
        Assert.Equal(CourtApiExtensions.BasePrice, after.ToCollect);
        Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(club.Token, booking.Id, "unpay")).StatusCode);
    }

    [Fact]
    public async Task ChangePaymentMethod_MovesTheMoneyBetweenCashAndTransfer()
    {
        var club = await SetUpClubAsync();
        var booking = await BookAsync(club, FirstSlot, paidWith: PaymentMethod.Cash);
        var unpaid = await BookAsync(club, SecondSlot, "Ana");

        var changed = await Http.WithBearer(club.Token).PutAsJsonAsync(
            $"{Routes.Bookings}/{booking.Id}/payment-method", new MarkPaidRequest(PaymentMethod.Transfer), Ct);
        var cash = await GetCashAsync(club);
        var rejected = await Http.PutAsJsonAsync($"{Routes.Bookings}/{unpaid.Id}/payment-method", new MarkPaidRequest(PaymentMethod.Cash), Ct);

        Assert.Equal(PaymentMethod.Transfer, (await changed.ReadBookingAsync(Ct)).PaymentMethod);
        Assert.Equal((0m, CourtApiExtensions.BasePrice), (cash.Cash, cash.Transfer));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task ResetAttendance_UndoesPlayedAndNoShow_ButNotWhenThereIsNothingToUndo()
    {
        var club = await SetUpClubAsync();
        var played = await BookAsync(club, FirstSlot);
        var noShow = await BookAsync(club, SecondSlot, "Ana");
        await PostAsync(club.Token, played.Id, "played");
        await PostAsync(club.Token, noShow.Id, "no-show");

        var first = await (await PostAsync(club.Token, played.Id, "attendance/reset")).ReadBookingAsync(Ct);
        var second = await (await PostAsync(club.Token, noShow.Id, "attendance/reset")).ReadBookingAsync(Ct);
        var again = await PostAsync(club.Token, played.Id, "attendance/reset");

        Assert.Equal((Attendance.Pending, Attendance.Pending), (first.Attendance, second.Attendance));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Changes_OnABookingOfAnotherClub_AreNotFound()
    {
        var a = await SetUpClubAsync();
        var b = await SetUpClubAsync();
        var booking = await BookAsync(a, FirstSlot, paidWith: PaymentMethod.Cash);

        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(b.Token, booking.Id, "unpay")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(b.Token, booking.Id, "attendance/reset")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.PutAsJsonAsync(
            $"{Routes.Bookings}/{booking.Id}/payment-method", new MarkPaidRequest(PaymentMethod.Transfer), Ct)).StatusCode);
    }

    [Fact]
    public async Task Suggestions_MatchNamesOfTheClub_WithTheirPhone_AndNeverOfAnotherClub()
    {
        var a = await SetUpClubAsync();
        var b = await SetUpClubAsync();
        await Http.WithBearer(a.Token).PostAsJsonAsync(
            Routes.Bookings, new CreateBookingRequest(a.CourtId, Day, FirstSlot, "Martina Pérez", "11 2345 6789", null, false, null), Ct);
        await BookAsync(a, SecondSlot, "Martín Gómez");
        await BookAsync(b, FirstSlot, "Martina Ajena");

        var suggestions = await SuggestAsync(a.Token, "mart");
        var onlyOne = await SuggestAsync(a.Token, "m");
        var wildcard = await SuggestAsync(a.Token, "%%");

        Assert.Equal(["Martín Gómez", "Martina Pérez"], suggestions.Select(s => s.Name).Order());
        Assert.Equal("11 2345 6789", suggestions.Single(s => s.Name == "Martina Pérez").Phone);
        Assert.Empty(onlyOne);
        Assert.Empty(wildcard);
    }

    [Fact]
    public async Task CreateCourt_WithBasePrice_CreatesTheBaseRuleInOneStep()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.Courts, new CourtRequest("Pádel 1", Sport.Padel, CourtApiExtensions.SlotMinutes, null, null, true, 18000m), Ct);
        var court = (await response.Content.ReadFromJsonAsync<CourtDto>(Ct))!;
        var rules = await Http.GetFromJsonAsync<List<ConCancha.Contracts.Pricing.PriceRuleDto>>($"{Routes.Courts}/{court.Id}/price-rules", Ct);
        var booking = await (await Http.BookAsync(owner.Token, court.Id, Day, FirstSlot, Ct)).ReadBookingAsync(Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(18000m, Assert.Single(rules!).Price);
        Assert.Equal(18000m, booking.Price);
    }

    [Fact]
    public async Task CreateCourt_WithAnInvalidBasePrice_IsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.Courts, new CourtRequest("Pádel 1", Sport.Padel, CourtApiExtensions.SlotMinutes, null, null, true, -1m), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<IReadOnlyList<CustomerSuggestionDto>> SuggestAsync(string token, string query) =>
        (await Http.WithBearer(token).GetFromJsonAsync<List<CustomerSuggestionDto>>(
            $"{Routes.Customers}/suggestions?q={Uri.EscapeDataString(query)}", Ct))!;
}
