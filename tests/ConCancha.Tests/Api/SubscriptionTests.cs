using System.Net;
using System.Net.Http.Json;
using ConCancha.Application.Emails;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Contracts.Setup;
using ConCancha.Contracts.Subscriptions;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Subscriptions;
using ConCancha.Infrastructure.Services;
using ConCancha.Tests.Support;

namespace ConCancha.Tests.Api;

/// <summary>The trial starts when the setup ends. A club whose trial is over keeps out of the panel until it is paid or given days.</summary>
[Collection(ApiCollection.Name)]
public class SubscriptionTests(ApiFactory api) : ApiTestBase(api)
{
    private const int Hour = 60;
    private const int Midnight = 24 * Hour;
    private static readonly TimeSpan TrialLength = TimeSpan.FromDays(SubscriptionRules.TrialDays);
    private static readonly TimeSpan PastTheTrial = TrialLength + TimeSpan.FromHours(1);
    private static readonly SetupAnswersDto Answers = new([new SportSetupDto(Sport.Padel, 1, 90, 24000m, null, null)], 8 * Hour, Midnight);

    [Fact]
    public async Task FinishingTheSetupStartsAFourteenDayTrial()
    {
        var owner = await OwnerWithFinishedSetupAsync();

        var subscription = await SubscriptionAsync(owner.Token);

        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
        Assert.True(subscription.HasAccess);
        Assert.Equal(SubscriptionRules.TrialDays, subscription.TrialDaysLeft);
    }

    [Fact]
    public async Task AClubStillSettingUpHasNoTrialYet()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var subscription = await SubscriptionAsync(owner.Token);

        Assert.Equal(new ClubSubscriptionDto(SubscriptionStatus.TrialNotStarted, true, null, null, null), subscription);
    }

    [Fact]
    public async Task WhenTheTrialIsOverThePanelAnswers402ButTheSubscriptionStillLoads()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        try
        {
            Api.Clock.Advance(PastTheTrial);

            var panel = await Http.WithBearer(owner.Token).GetAsync(Routes.Courts, Ct);
            var subscription = await SubscriptionAsync(owner.Token);

            Assert.Equal(HttpStatusCode.PaymentRequired, panel.StatusCode);
            Assert.Equal(SubscriptionStatus.TrialExpired, subscription.Status);
            Assert.False(subscription.HasAccess);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task ExtendingTheTrialReopensThePanelWithTheDaysAddedFromToday()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();
        try
        {
            Api.Clock.Advance(PastTheTrial);

            var extended = await platform.PostAsJsonAsync(ClubRoute(owner) + "/trial/extension", new ExtendTrialRequest(7), Ct);
            var subscription = await SubscriptionAsync(owner.Token);
            var panel = await Http.WithBearer(owner.Token).GetAsync(Routes.Courts, Ct);

            Assert.Equal(HttpStatusCode.NoContent, extended.StatusCode);
            Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
            Assert.Equal(7, subscription.TrialDaysLeft);
            Assert.Equal(HttpStatusCode.OK, panel.StatusCode);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task RecordingAPaymentReopensThePanelForThirtyDays()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();
        try
        {
            Api.Clock.Advance(PastTheTrial);

            var paid = await platform.PostAsync(ClubRoute(owner) + "/subscription/payment", null, Ct);
            var subscription = await SubscriptionAsync(owner.Token);
            var panel = await Http.WithBearer(owner.Token).GetAsync(Routes.Courts, Ct);

            Assert.Equal(HttpStatusCode.NoContent, paid.StatusCode);
            Assert.Equal(SubscriptionStatus.Subscribed, subscription.Status);
            Assert.Equal(LocalToday().AddDays(SubscriptionRules.PaidDays), subscription.PaidUntilOn);
            Assert.Equal(HttpStatusCode.OK, panel.StatusCode);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task ASubscriptionThatRanOutKeepsTheClubOpenUntilThePaymentIsRecorded()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();
        try
        {
            await platform.PostAsync(ClubRoute(owner) + "/subscription/payment", null, Ct);
            Api.Clock.Advance(TimeSpan.FromDays(SubscriptionRules.PaidDays + 1));

            var subscription = await SubscriptionAsync(owner.Token);
            var panel = await Http.WithBearer(owner.Token).GetAsync(Routes.Courts, Ct);

            Assert.Equal(SubscriptionStatus.PaymentPending, subscription.Status);
            Assert.True(subscription.HasAccess);
            Assert.Equal(HttpStatusCode.OK, panel.StatusCode);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task RegularizationEmailReachesTheOwnerOnlyWhenSomethingIsOverdue()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();
        try
        {
            var tooEarly = await platform.PostAsync(ClubRoute(owner) + "/subscription/regularization-email", null, Ct);
            Api.Clock.Advance(PastTheTrial);

            var sent = await platform.PostAsync(ClubRoute(owner) + "/subscription/regularization-email", null, Ct);

            var email = Api.Emails.Sent.Last(message => message.To == owner.User.Email);
            Assert.Equal(HttpStatusCode.Conflict, tooEarly.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
            Assert.Equal(EmailTemplates.SubscriptionPending(owner.Club!.Name).Subject, email.Subject);
        }
        finally
        {
            Api.Clock.Reset();
        }
    }

    [Fact]
    public async Task ExtendingBeforeTheTrialStartsIsAConflict()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var platform = await PlatformClientAsync();

        var response = await platform.PostAsJsonAsync(ClubRoute(owner) + "/trial/extension", new ExtendTrialRequest(7), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task ExtensionsOutsideOneDayToAYearAreRejected(int days)
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();

        var response = await platform.PostAsJsonAsync(ClubRoute(owner) + "/trial/extension", new ExtendTrialRequest(days), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlatformActionsOnAnUnknownClubAreNotFound()
    {
        var platform = await PlatformClientAsync();
        var unknown = $"api/platform/clubs/{Guid.NewGuid()}";

        var extended = await platform.PostAsJsonAsync(unknown + "/trial/extension", new ExtendTrialRequest(7), Ct);
        var paid = await platform.PostAsync(unknown + "/subscription/payment", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, extended.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, paid.StatusCode);
    }

    [Fact]
    public async Task TheOverviewShowsEachClubsTrialDaysLeft()
    {
        var owner = await OwnerWithFinishedSetupAsync();
        var platform = await PlatformClientAsync();

        var overview = await platform.GetFromJsonAsync<PlatformOverviewDto>("api/platform/overview", Ct);

        var row = overview!.Clubs.Single(club => club.Id == owner.Club!.Id);
        Assert.Equal(SubscriptionStatus.Trial, row.Subscription.Status);
        Assert.Equal(SubscriptionRules.TrialDays, row.Subscription.TrialDaysLeft);
    }

    private async Task<SessionResponse> OwnerWithFinishedSetupAsync()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var setup = await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.SetupComplete, Answers, Ct);
        setup.EnsureSuccessStatusCode();
        return owner;
    }

    private async Task<ClubSubscriptionDto> SubscriptionAsync(string token) =>
        (await Http.WithBearer(token).GetFromJsonAsync<ClubSubscriptionDto>(Routes.ClubSubscription, Ct))!;

    private async Task<HttpClient> PlatformClientAsync()
    {
        var token = await PlatformTestData.LoginAsPlatformAdminAsync(Api.CreateClient(), Ct);
        return Api.CreateClient().WithBearer(token);
    }

    private static string ClubRoute(SessionResponse owner) => $"api/platform/clubs/{owner.Club!.Id}";

    /// <summary>Today in Argentina by the test clock, which the tests move forward.</summary>
    private DateOnly LocalToday() => PlatformReporting.Zone.ToLocal(Api.Clock.GetUtcNow()).Date;
}
