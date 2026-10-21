using System.Net;
using System.Net.Http.Json;
using ConCancha.Contracts.Courts;
using ConCancha.Contracts.Pricing;
using ConCancha.Contracts.Setup;
using ConCancha.Domain.Courts;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

public class SetupTests(ApiFactory api) : ApiTestBase(api)
{
    private const int Hour = 60;
    private const int Midnight = 24 * Hour;

    private static SetupAnswersDto Answers(params SportSetupDto[] sports) => new(sports, 8 * Hour, Midnight);

    private static SportSetupDto Padel(int courts = 2, decimal? night = 30000m) =>
        new(Sport.Padel, courts, 90, 24000m, night, night is null ? null : 18 * Hour);

    [Fact]
    public async Task GetProgress_NewClub_StartsAtTheBeginning()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var progress = await Http.WithBearer(owner.Token).GetFromJsonAsync<SetupProgressDto>(Routes.Setup, Ct);

        Assert.Equal(new SetupProgressDto(0, null, false), progress);
    }

    [Fact]
    public async Task SaveProgress_IsResumedLater()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var draft = Answers(Padel());

        var save = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.Setup, new SaveSetupProgressRequest(3, draft), Ct);
        var progress = await Http.GetFromJsonAsync<SetupProgressDto>(Routes.Setup, Ct);

        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);
        Assert.Equal(3, progress!.Step);
        Assert.Equal(2, progress.Draft!.Sports[0].CourtCount);
        Assert.False(progress.IsCompleted);
    }

    [Fact]
    public async Task Complete_CreatesNamedCourtsWithBaseAndNightPrices()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var football = new SportSetupDto(Sport.FiveASideFootball, 1, 60, 18000m, null, null);

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.SetupComplete, Answers(Padel(), football), Ct);
        var courts = await Http.GetFromJsonAsync<List<CourtDto>>(Routes.Courts, Ct);
        var padelRules = await Http.GetFromJsonAsync<List<PriceRuleDto>>($"{Routes.Courts}/{courts![0].Id}/price-rules", Ct);
        var footballRules = await Http.GetFromJsonAsync<List<PriceRuleDto>>($"{Routes.Courts}/{courts[2].Id}/price-rules", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["Pádel 1", "Pádel 2", "Fútbol 5 1"], courts.Select(c => c.Name));
        Assert.Equal([24000m, 30000m], padelRules!.Select(r => r.Price));
        Assert.Equal(18 * Hour, padelRules![1].FromMinuteOfDay);
        Assert.Equal([18000m], footballRules!.Select(r => r.Price));
    }

    [Fact]
    public async Task Complete_SetsClubHours_AndMarksTheSetupAsDone()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var answers = new SetupAnswersDto([Padel()], 10 * Hour, 23 * Hour);

        await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.SetupComplete, answers, Ct);
        var club = await Http.GetFromJsonAsync<Contracts.Clubs.ClubDto>(Routes.CurrentClub, Ct);
        var progress = await Http.GetFromJsonAsync<SetupProgressDto>(Routes.Setup, Ct);
        var again = await Http.PostAsJsonAsync(Routes.SetupComplete, answers, Ct);

        Assert.Equal((10 * Hour, 23 * Hour), (club!.OpeningMinuteOfDay, club.ClosingMinuteOfDay));
        Assert.True(progress!.IsCompleted);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Complete_ThenTheAgendaIsReadyToUse()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.SetupComplete, Answers(Padel(1)), Ct);

        var slots = (await Http.GetAgendaAsync(owner.Token, CourtApiExtensions.FutureDate(), Ct)).Courts.Single().Slots;

        Assert.All(slots, slot => Assert.NotNull(slot.Price));
    }

    [Fact]
    public async Task Complete_InvalidAnswers_CreateNothing()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var noPrice = new SportSetupDto(Sport.Padel, 1, 90, null, null, null);
        var halfNight = new SportSetupDto(Sport.Padel, 1, 90, 100m, 200m, null);
        var tooMany = Padel(courts: 21);
        var badSlot = new SportSetupDto(Sport.Padel, 1, 45, 100m, null, null);

        foreach (var invalid in new[] { Answers(), Answers(noPrice), Answers(halfNight), Answers(tooMany), Answers(badSlot), Answers(Padel(), Padel()) })
        {
            var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.SetupComplete, invalid, Ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.Empty((await Http.GetFromJsonAsync<List<CourtDto>>(Routes.Courts, Ct))!);
    }

    [Fact]
    public async Task Setup_IsOwnerOnly()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var staff = await CreateStaffSessionAsync(owner.Club!.Id);

        var response = await Http.WithBearer(staff.Token).GetAsync(Routes.Setup, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
