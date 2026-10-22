using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

/// <summary>Module M3: the owner edits the club's brand, the cancellation window and their own password.</summary>
public class ClubSettingsTests(ApiFactory api) : ApiTestBase(api)
{
    private const string NewPassword = "otra-clave-larga-456";
    private const int HoursOutsideTheRange = BookingPolicy.MaxCancellationWindowHours + 1;

    private static DateOnly Soon => CourtApiExtensions.FutureDate(3);

    [Fact]
    public async Task UpdateProfile_ChangesNameAndPhone_ButNotTheSlug()
    {
        var slug = ApiClientExtensions.NewSlug();
        var owner = await Http.RegisterWithClubAsync(Ct, "Nombre viejo", slug);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubProfile, new UpdateClubProfileRequest("  Nombre nuevo ", " 221 555 0000 "), Ct);
        var club = await Http.GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Nombre nuevo", club!.Name);
        Assert.Equal("221 555 0000", club.Phone);
        Assert.Equal(slug, club.Slug);
    }

    [Fact]
    public async Task UpdateProfile_BlankPhone_ClearsIt()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubProfile, new UpdateClubProfileRequest("Club", "221 555 0000"), Ct);

        var response = await Http.PutAsJsonAsync(Routes.ClubProfile, new UpdateClubProfileRequest("Club", "   "), Ct);

        Assert.Null((await response.Content.ReadFromJsonAsync<ClubDto>(Ct))!.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task UpdateProfile_WithoutAValidName_Returns400AndKeepsTheOldOne(string name)
    {
        var owner = await Http.RegisterWithClubAsync(Ct, "Nombre viejo");

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubProfile, new UpdateClubProfileRequest(name, null), Ct);
        var club = await Http.GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Nombre viejo", club!.Name);
    }

    [Fact]
    public async Task UpdateProfile_OnlyTouchesTheClubOfTheSession()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A");
        var b = await Http.RegisterWithClubAsync(Ct, "Club B");

        await Http.WithBearer(a.Token).PutAsJsonAsync(Routes.ClubProfile, new UpdateClubProfileRequest("Club A renombrado", null), Ct);
        await Http.WithBearer(a.Token).PutAsJsonAsync(Routes.ClubCancellation, new UpdateCancellationRequest(48), Ct);
        var clubB = await Http.WithBearer(b.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal("Club B", clubB!.Name);
        Assert.Equal(BookingPolicy.DefaultCancellationWindowHours, clubB.CancellationWindowHours);
    }

    [Fact]
    public async Task UpdateCancellation_IsReadBackAndShownOnThePublicSite()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubCancellation, new UpdateCancellationRequest(24), Ct);
        var publicClub = await Http.WithBearer(null).GetFromJsonAsync<PublicClubDto>($"{Routes.PublicClubs}/{owner.Club!.Slug}", Ct);

        Assert.Equal(24, (await response.Content.ReadFromJsonAsync<ClubDto>(Ct))!.CancellationWindowHours);
        Assert.Equal(24, publicClub!.CancellationWindowHours);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(HoursOutsideTheRange)]
    public async Task UpdateCancellation_OutsideTheRange_Returns400(int hours)
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubCancellation, new UpdateCancellationRequest(hours), Ct);
        var club = await Http.GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(BookingPolicy.DefaultCancellationWindowHours, club!.CancellationWindowHours);
    }

    [Fact]
    public async Task UpdateCancellation_NewWindowStopsThePlayerFromCancellingInsideIt()
    {
        var slug = ApiClientExtensions.NewSlug();
        var owner = await Http.RegisterWithClubAsync(Ct, slug: slug);
        var court = await Http.CreateCourtAsync(owner.Token, Ct);
        await Http.CreateBasePriceAsync(owner.Token, court.Id, Ct);
        var created = await (await Http.PublicBookAsync(slug, court.Id, Soon, CourtApiExtensions.FirstSlotMinute, Ct)).ReadCreatedAsync(Ct);
        var cancelUrl = PublicApiExtensions.PublicUrl(slug, $"bookings/{created.ManagementToken}/cancel");

        await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubCancellation, new UpdateCancellationRequest(BookingPolicy.MaxCancellationWindowHours), Ct);
        var response = await Http.WithBearer(null).PostAsync(cancelUrl, null, Ct);

        Assert.True(created.Booking.CanCancel);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NewOneWorksAndOldOneStopsWorking()
    {
        var email = ApiClientExtensions.NewEmail();
        var owner = await RegisterOwnerAsync(email);

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.ChangePassword, new ChangePasswordRequest(ApiClientExtensions.ValidPassword, NewPassword), Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Http.LoginAsync(email, NewPassword, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400OnThatField()
    {
        var email = ApiClientExtensions.NewEmail();
        var owner = await RegisterOwnerAsync(email);

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(Routes.ChangePassword, new ChangePasswordRequest("no-es-esa-123456", NewPassword), Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("currentPassword", problem!.Errors.Keys);
        Assert.Equal(HttpStatusCode.OK, (await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_TooShort_Returns400OnTheNewPasswordField()
    {
        var owner = await RegisterOwnerAsync(ApiClientExtensions.NewEmail());

        var response = await Http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.ChangePassword, new ChangePasswordRequest(ApiClientExtensions.ValidPassword, "corta"), Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("newPassword", problem!.Errors.Keys);
    }

    [Fact]
    public async Task ChangePassword_LeakedPassword_IsRejected()
    {
        const string leaked = "contrasena-filtrada-1";
        using var checking = Api.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:CheckLeakedPasswords"] = "true" }));
            builder.ConfigureServices(services => services
                .AddHttpClient(LeakedPasswordValidator.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new LeakedRangeHandler(leaked)));
        });
        var http = checking.CreateClient();
        var owner = await http.RegisterWithClubAsync(Ct);

        var response = await http.WithBearer(owner.Token).PostAsJsonAsync(
            Routes.ChangePassword, new ChangePasswordRequest(ApiClientExtensions.ValidPassword, leaked), Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("filtradas", problem!.Errors["newPassword"][0]);
    }

    private async Task<SessionResponse> RegisterOwnerAsync(string email)
    {
        var account = await Http.RegisterAsync(Ct, email);
        var response = await Http.CreateClubAsync(account.Token, new CreateClubRequest("Club de prueba", ApiClientExtensions.NewSlug(), null), Ct);
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;
    }

    /// <summary>Answers the range endpoint as if only <c>leakedPassword</c> had been found in a leak.</summary>
    private sealed class LeakedRangeHandler(string leakedPassword) : HttpMessageHandler
    {
        private const int HashPrefixLength = 5;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(leakedPassword)));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{hash[HashPrefixLength..]}:42") });
        }
    }
}
