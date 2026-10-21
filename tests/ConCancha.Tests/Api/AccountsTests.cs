using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Accounts;
using ConCancha.Domain.Clubs;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Tests.Api;

public class AccountsTests(ApiFactory api) : ApiTestBase(api)
{
    private const string WrongPassword = "otra-clave-larga-1";
    private const string NewPassword = "contrasena-nueva-456";
    private const string AnotherNewPassword = "otra-clave-larga-789";
    private const string ForgedResetToken = "dG9rZW4";
    private static readonly string ResetLinkPrefix = $"{ApiFactory.WebBaseUrl}/panel/restablecer";
    private static readonly Regex TokenInLink = new(@"token=([^\s&]+)");

    [Fact]
    public async Task Register_CreatesAccountAndReturnsSessionWithoutClub()
    {
        var session = await Http.RegisterAsync(Ct);

        Assert.False(string.IsNullOrEmpty(session.Token));
        Assert.Null(session.Club);
        Assert.Null(session.Role);
    }

    [Fact]
    public async Task Register_WithRepeatedEmail_Returns409EvenIfCasingChanges()
    {
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        var response = await Http.PostAsJsonAsync(
            Routes.Register, new RegisterRequest(email.ToUpperInvariant(), ApiClientExtensions.ValidPassword), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400WithSpanishMessage()
    {
        var response = await Http.PostAsJsonAsync(
            Routes.Register, new RegisterRequest(ApiClientExtensions.NewEmail(), "corta"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains($"al menos {AccountLimits.PasswordMinLength} caracteres", problem!.Errors["password"][0]);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var response = await Http.PostAsJsonAsync(
            Routes.Register, new RegisterRequest("no-es-un-mail", ApiClientExtensions.ValidPassword), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsOwnersClubAndRole()
    {
        var email = ApiClientExtensions.NewEmail();
        var account = await Http.RegisterAsync(Ct, email);
        await Http.CreateClubAsync(account.Token, new CreateClubRequest("Los Pinos", ApiClientExtensions.NewSlug(), null), Ct);
        Http.WithBearer(null);

        var response = await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<SessionResponse>(Ct))!;
        Assert.Equal("Los Pinos", session.Club!.Name);
        Assert.Equal(nameof(ClubRole.Owner), session.Role);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_ReturnsSame401()
    {
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        var wrongPassword = await Http.LoginAsync(email, WrongPassword, Ct);
        var unknownEmail = await Http.LoginAsync(ApiClientExtensions.NewEmail(), ApiClientExtensions.ValidPassword, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await ReadTitleAsync(wrongPassword), await ReadTitleAsync(unknownEmail));
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccountEvenWithCorrectPassword()
    {
        const int maxFailedAttempts = 5;
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        for (var attempt = 0; attempt < maxFailedAttempts; attempt++)
            await Http.LoginAsync(email, WrongPassword, Ct);

        var response = await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct);
        Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AllowsNewPasswordAndLinkWorksOnlyOnce()
    {
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        var forgot = await Http.PostAsJsonAsync(Routes.ForgotPassword, new ForgotPasswordRequest(email), Ct);
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        var token = ExtractResetToken(email);

        var reset = await Http.PostAsJsonAsync(Routes.ResetPassword, new ResetPasswordRequest(email, token, NewPassword), Ct);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Http.LoginAsync(email, NewPassword, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.LoginAsync(email, ApiClientExtensions.ValidPassword, Ct)).StatusCode);

        var reuse = await Http.PostAsJsonAsync(Routes.ResetPassword, new ResetPasswordRequest(email, token, AnotherNewPassword), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_DoesNotRevealWhetherAccountExists()
    {
        var sentBefore = Api.Emails.Sent.Count;

        var response = await Http.PostAsJsonAsync(
            Routes.ForgotPassword, new ForgotPasswordRequest(ApiClientExtensions.NewEmail()), Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(sentBefore, Api.Emails.Sent.Count);
    }

    [Fact]
    public async Task ResetPassword_WithForgedToken_Returns400()
    {
        var email = ApiClientExtensions.NewEmail();
        await Http.RegisterAsync(Ct, email);

        var response = await Http.PostAsJsonAsync(
            Routes.ResetPassword, new ResetPasswordRequest(email, ForgedResetToken, NewPassword), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReadyAnswersWithRealDatabase()
    {
        Assert.Equal(HttpStatusCode.OK, (await Http.GetAsync(Routes.Health, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Http.GetAsync(Routes.HealthReady, Ct)).StatusCode);
    }

    [Fact]
    public async Task Swagger_PublishesV1Document() =>
        Assert.Equal(HttpStatusCode.OK, (await Http.GetAsync(Routes.SwaggerDocument, Ct)).StatusCode);

    private string ExtractResetToken(string email)
    {
        var message = Api.Emails.Sent.Last(sent => sent.To == email);
        Assert.StartsWith(ResetLinkPrefix, message.Body.Split('\n')[1]);
        return TokenInLink.Match(message.Body).Groups[1].Value;
    }

    private async Task<string?> ReadTitleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct))!.Title;
}
