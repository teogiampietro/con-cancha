using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Bookings;
using ConCancha.Contracts.Clubs;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Contracts.RecurringBookings;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Courts;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services;
using ConCancha.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

/// <summary>Module M11: weekly fixed turns that fill 12 weeks of the agenda and renew themselves.</summary>
public class RecurringBookingsTests(ApiFactory api) : ApiTestBase(api)
{
    private const int FirstSlot = CourtApiExtensions.FirstSlotMinute;
    private const int ParallelCalls = 8;
    private const int ShorterSlotMinutes = 60;
    private const int PastWeeks = 2;
    private const decimal RaisedPrice = 25000m;
    private const string FixedCustomer = "Los de los martes";
    private const string RenamedCustomer = "Los de siempre";

    private sealed record Club(string Token, Guid ClubId, Guid CourtId, Guid PriceRuleId);

    /// <summary>Two days ahead in UTC is at least one day ahead in any club's local date, so all 12 occurrences are in the future.</summary>
    private static DayOfWeek FixedDay => DateTime.UtcNow.AddDays(2).DayOfWeek;

    private string RecurringUrl(Club club) => $"{Routes.Courts}/{club.CourtId}/recurring-bookings";

    private async Task<Club> SetUpClubAsync()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        var rule = await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        return new Club(owner.Token, owner.Club!.Id, court.Id, rule.Id);
    }

    private Task<HttpResponseMessage> CreateRecurringAsync(Club club, int startMinute = FirstSlot, string name = FixedCustomer) =>
        Http.WithBearer(club.Token).PostAsJsonAsync(
            RecurringUrl(club), new RecurringBookingRequest(FixedDay, startMinute, name, null), Ct);

    private async Task<RecurringBookingResultDto> CreateRecurringOkAsync(Club club)
    {
        var response = await CreateRecurringAsync(club);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RecurringBookingResultDto>(Ct))!;
    }

    private async Task<List<Booking>> BookingsOfAsync(Guid recurringId)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        return await db.Bookings.IgnoreQueryFilters().AsNoTracking()
            .Where(booking => booking.RecurringBookingId == recurringId).OrderBy(booking => booking.StartsAt).ToListAsync(Ct);
    }

    private async Task<List<Booking>> UpcomingAsync(Guid recurringId) =>
        (await BookingsOfAsync(recurringId)).Where(booking => booking.Status == BookingStatus.Confirmed).ToList();

    private Task RenewAsync() => Api.Services.GetRequiredService<RecurringBookingRenewal>().RenewAllAsync(Ct);

    [Fact]
    public async Task Create_FillsTwelveWeeksOfTheAgenda()
    {
        var club = await SetUpClubAsync();

        var result = await CreateRecurringOkAsync(club);

        Assert.Equal(RecurringSchedule.WindowWeeks, result.CreatedCount);
        Assert.Empty(result.SkippedDates);
        var bookings = await BookingsOfAsync(result.Recurring.Id);
        Assert.Equal(RecurringSchedule.WindowWeeks, bookings.Count);
        Assert.All(bookings, booking =>
        {
            Assert.Equal(BookingKind.Reservation, booking.Kind);
            Assert.Equal(CourtApiExtensions.BasePrice, booking.Price);
            Assert.Equal(FixedCustomer, booking.CustomerName);
        });
    }

    [Fact]
    public async Task Create_TheFixedTurnShowsInTheAgendaAsFixed()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var first = (await BookingsOfAsync(result.Recurring.Id))[0];
        var date = DateOnly.FromDateTime(first.StartsAt.UtcDateTime.AddHours(-3));

        var agenda = await Http.GetAgendaAsync(club.Token, date, Ct);

        var booking = agenda.Courts.SelectMany(court => court.Slots).Select(slot => slot.Booking).OfType<BookingDto>().Single();
        Assert.Equal(result.Recurring.Id, booking.RecurringBookingId);
    }

    [Fact]
    public async Task Renewal_IsIdempotent()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);

        await RenewAsync();
        await RenewAsync();

        Assert.Equal(RecurringSchedule.WindowWeeks, (await BookingsOfAsync(result.Recurring.Id)).Count);
    }

    [Fact]
    public async Task Renewal_RefillsDatesThatLeftTheWindow()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        await RemoveBookingsAsync(result.Recurring.Id, take: 2);

        await RenewAsync();

        Assert.Equal(RecurringSchedule.WindowWeeks, (await BookingsOfAsync(result.Recurring.Id)).Count);
    }

    [Fact]
    public async Task Renewal_RunningInParallelNeverDuplicatesATurn()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        await RemoveBookingsAsync(result.Recurring.Id, take: 3);

        await Task.WhenAll(Enumerable.Range(0, ParallelCalls).Select(_ => RenewAsync()));

        var bookings = await BookingsOfAsync(result.Recurring.Id);
        Assert.Equal(RecurringSchedule.WindowWeeks, bookings.Count);
        Assert.Equal(bookings.Count, bookings.Select(booking => booking.StartsAt).Distinct().Count());
    }

    [Fact]
    public async Task Renewal_DoesNotRecreateADateTheClubCancelled()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var first = (await BookingsOfAsync(result.Recurring.Id))[0];
        await Http.WithBearer(club.Token).PostAsync($"{Routes.Bookings}/{first.Id}/cancel", null, Ct);

        await RenewAsync();

        var bookings = await BookingsOfAsync(result.Recurring.Id);
        Assert.Equal(RecurringSchedule.WindowWeeks, bookings.Count);
        Assert.Single(bookings, booking => booking.Status == BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Create_ADateAlreadyTakenIsReportedAndNotOverwritten()
    {
        var club = await SetUpClubAsync();
        var taken = await TakeFirstOccurrenceAsync(club);

        var result = await CreateRecurringOkAsync(club);

        Assert.Equal(RecurringSchedule.WindowWeeks - 1, result.CreatedCount);
        Assert.Equal([taken.Date], result.SkippedDates);
        var bookings = await BookingsOfAsync(result.Recurring.Id);
        Assert.DoesNotContain(bookings, booking => booking.StartsAt == taken.StartsAt);
        var kept = await Http.WithBearer(club.Token).GetFromJsonAsync<BookingDto>($"{Routes.Bookings}/{taken.Id}", Ct);
        Assert.Equal(BookingStatus.Confirmed, kept!.Status);
        Assert.Null(kept.RecurringBookingId);
    }

    [Fact]
    public async Task Create_TheSameCellInParallel_OnlyOneWins()
    {
        var club = await SetUpClubAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, ParallelCalls).Select(_ => CreateRecurringAsync(club)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(ParallelCalls - 1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        var listed = await Http.WithBearer(club.Token).GetFromJsonAsync<List<RecurringBookingDto>>(RecurringUrl(club), Ct);
        Assert.Single(listed!);
    }

    [Fact]
    public async Task Create_WithoutAPriceForThatSlot_IsRejected()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        var club = new Club(owner.Token, owner.Club!.Id, court.Id, Guid.Empty);

        var response = await CreateRecurringAsync(club);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AtAnHourThatIsNotASlot_IsRejected()
    {
        var club = await SetUpClubAsync();

        var response = await CreateRecurringAsync(club, startMinute: FirstSlot + 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CancelsUpcomingTurnsAndKeepsThePastOnes()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var past = await AddPastBookingAsync(club, result.Recurring.Id);

        var response = await Http.WithBearer(club.Token).DeleteAsync($"{RecurringUrl(club)}/{result.Recurring.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var kept = await GetBookingAsync(club, past.Id);
        Assert.Equal(BookingStatus.Confirmed, kept.Status);
        Assert.Empty(await UpcomingAsync(result.Recurring.Id));
        Assert.Equal(RecurringSchedule.WindowWeeks, await CountCancelledByCourtAsync(club.CourtId));
    }

    [Fact]
    public async Task Delete_ThenCreatingTheSameCellAgain_FillsTheWindowAgain()
    {
        var club = await SetUpClubAsync();
        var first = await CreateRecurringOkAsync(club);
        await Http.WithBearer(club.Token).DeleteAsync($"{RecurringUrl(club)}/{first.Recurring.Id}", Ct);

        var second = await CreateRecurringOkAsync(club);

        Assert.Equal(RecurringSchedule.WindowWeeks, second.CreatedCount);
    }

    [Fact]
    public async Task Update_ChangesTheCustomerOfUpcomingTurnsAndKeepsWhatWasPaid()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var paid = (await BookingsOfAsync(result.Recurring.Id))[1];
        await Http.WithBearer(club.Token).PostAsJsonAsync($"{Routes.Bookings}/{paid.Id}/pay", new MarkPaidRequest(PaymentMethod.Cash), Ct);

        var response = await Http.WithBearer(club.Token).PutAsJsonAsync(
            $"{RecurringUrl(club)}/{result.Recurring.Id}", new UpdateRecurringBookingRequest(RenamedCustomer, null), Ct);

        response.EnsureSuccessStatusCode();
        var bookings = await UpcomingAsync(result.Recurring.Id);
        Assert.Equal(RecurringSchedule.WindowWeeks, bookings.Count);
        Assert.All(bookings, booking => Assert.Equal(RenamedCustomer, booking.CustomerName));
        Assert.Equal(PaymentStatus.Paid, bookings.Single(booking => booking.Id == paid.Id).PaymentStatus);
    }

    [Fact]
    public async Task ChangingThePriceOfTheSlot_RepricesUpcomingUnpaidTurnsOnly()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var paid = (await BookingsOfAsync(result.Recurring.Id))[0];
        await Http.WithBearer(club.Token).PostAsJsonAsync($"{Routes.Bookings}/{paid.Id}/pay", new MarkPaidRequest(PaymentMethod.Cash), Ct);

        var response = await Http.WithBearer(club.Token).PutAsJsonAsync(
            $"{Routes.PriceRules}/{club.PriceRuleId}", new PriceRuleRequest(null, 0, 24 * 60, RaisedPrice), Ct);

        response.EnsureSuccessStatusCode();
        var bookings = await UpcomingAsync(result.Recurring.Id);
        Assert.Equal(CourtApiExtensions.BasePrice, bookings.Single(booking => booking.Id == paid.Id).Price);
        Assert.All(bookings.Where(booking => booking.Id != paid.Id), booking => Assert.Equal(RaisedPrice, booking.Price));
    }

    [Fact]
    public async Task ChangingTheSlotLengthOfTheCourt_PausesItsFixedTurnsAndCancelsUpcomingOnes()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);

        var update = new CourtRequest("Pádel 1", Sport.Padel, ShorterSlotMinutes, null, null);
        (await Http.WithBearer(club.Token).PutAsJsonAsync($"{Routes.Courts}/{club.CourtId}", update, Ct)).EnsureSuccessStatusCode();

        var listed = await Http.WithBearer(club.Token).GetFromJsonAsync<List<RecurringBookingDto>>(RecurringUrl(club), Ct);
        Assert.False(listed!.Single().IsActive);
        Assert.Empty(await UpcomingAsync(result.Recurring.Id));
        await RenewAsync();
        Assert.Empty(await UpcomingAsync(result.Recurring.Id));
    }

    [Fact]
    public async Task ChangingTheClubHours_PausesOnlyTheFixedTurnsLeftOutsideTheGrid()
    {
        var club = await SetUpClubAsync();
        var before = await CreateRecurringOkAsync(club);
        var later = await Http.WithBearer(club.Token).PostAsJsonAsync(
            RecurringUrl(club), new RecurringBookingRequest(FixedDay, FirstSlot + 2 * CourtApiExtensions.SlotMinutes, "Otro", null), Ct);
        later.EnsureSuccessStatusCode();

        var hours = new UpdateClubHoursRequest(FirstSlot + CourtApiExtensions.SlotMinutes, 24 * 60);
        (await Http.WithBearer(club.Token).PutAsJsonAsync(Routes.ClubHours, hours, Ct)).EnsureSuccessStatusCode();

        var listed = await Http.WithBearer(club.Token).GetFromJsonAsync<List<RecurringBookingDto>>(RecurringUrl(club), Ct);
        Assert.Equal([before.Recurring.Id], listed!.Where(item => !item.IsActive).Select(item => item.Id));
        Assert.Single(listed!, item => item.IsActive);
    }

    [Fact]
    public async Task AnotherClub_CannotSeeChangeOrDeleteTheFixedTurns()
    {
        var club = await SetUpClubAsync();
        var result = await CreateRecurringOkAsync(club);
        var intruder = await Http.RegisterWithClubAsync(Ct, "Otro club", ApiClientExtensions.NewSlug());
        var http = Http.WithBearer(intruder.Token);
        var url = RecurringUrl(club);

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsJsonAsync(url, new RecurringBookingRequest(FixedDay, FirstSlot, "Intruso", null), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PutAsJsonAsync($"{url}/{result.Recurring.Id}", new UpdateRecurringBookingRequest("Intruso", null), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.DeleteAsync($"{url}/{result.Recurring.Id}", Ct)).StatusCode);
        Assert.Equal(RecurringSchedule.WindowWeeks, (await UpcomingAsync(result.Recurring.Id)).Count);
    }

    [Fact]
    public async Task RecurringBookings_AreFilteredByClubInTheDatabase()
    {
        var club = await SetUpClubAsync();
        await CreateRecurringOkAsync(club);
        var other = await SetUpClubAsync();

        using var scope = Api.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<ConCancha.Application.Abstractions.ITenantContext>();
        tenant.SetClub(other.ClubId);
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();

        Assert.Empty(await db.RecurringBookings.ToListAsync(Ct));
        Assert.Empty(await db.Bookings.Where(booking => booking.RecurringBookingId != null).ToListAsync(Ct));
    }

    private async Task RemoveBookingsAsync(Guid recurringId, int take)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var last = await db.Bookings.IgnoreQueryFilters().Where(booking => booking.RecurringBookingId == recurringId)
            .OrderByDescending(booking => booking.StartsAt).Take(take).ToListAsync(Ct);
        db.Bookings.RemoveRange(last);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<(Guid Id, DateOnly Date, DateTimeOffset StartsAt)> TakeFirstOccurrenceAsync(Club club)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var booking = await (await Http.BookAsync(club.Token, club.CourtId, date, FirstSlot, Ct)).ReadBookingAsync(Ct);
        return (booking.Id, date, booking.StartsAt);
    }

    private async Task<Booking> AddPastBookingAsync(Club club, Guid recurringId)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var startsAt = DateTimeOffset.UtcNow.AddDays(-PastWeeks * RecurringSchedule.DaysPerWeek);
        var past = new Booking
        {
            ClubId = club.ClubId, CourtId = club.CourtId, StartsAt = startsAt, EndsAt = startsAt.AddMinutes(CourtApiExtensions.SlotMinutes),
            Kind = BookingKind.Reservation, CustomerName = FixedCustomer, Price = CourtApiExtensions.BasePrice, RecurringBookingId = recurringId,
        };
        db.Bookings.Add(past);
        await db.SaveChangesAsync(Ct);
        return past;
    }

    private async Task<BookingDto> GetBookingAsync(Club club, Guid bookingId) =>
        (await Http.WithBearer(club.Token).GetFromJsonAsync<BookingDto>($"{Routes.Bookings}/{bookingId}", Ct))!;

    private async Task<int> CountCancelledByCourtAsync(Guid courtId)
    {
        using var scope = Api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        return await db.Bookings.IgnoreQueryFilters()
            .CountAsync(booking => booking.CourtId == courtId && booking.Status == BookingStatus.Cancelled, Ct);
    }
}
