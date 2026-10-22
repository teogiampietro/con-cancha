using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Api;

/// <summary>Module M18: logo, colors and optional sections of a club.</summary>
public class ClubAppearanceTests(ApiFactory api) : ApiTestBase(api)
{
    private const string BrightRed = "#e63946";
    private const string DarkBlue = "#0B1020";
    private const int OneMegabyte = 1024 * 1024;

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];

    [Fact]
    public async Task UpdateColors_AreSavedAndReachThePublicSite()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubColors, new UpdateClubColorsRequest(BrightRed, "#3f7d5a"), Ct);
        var publicClub = await Http.WithBearer(null).GetFromJsonAsync<PublicClubDto>($"{Routes.PublicClubs}/{owner.Club!.Slug}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("#E63946", (await response.Content.ReadFromJsonAsync<ClubDto>(Ct))!.PrimaryColor);
        Assert.Equal("#E63946", publicClub!.PrimaryColor);
        Assert.Equal("#3F7D5A", publicClub.SecondaryColor);
    }

    [Theory]
    [InlineData("rojo")]
    [InlineData("#12345")]
    [InlineData(DarkBlue)]
    public async Task UpdateColors_InvalidOrUnreadable_Returns400OnThatFieldAndKeepsTheOldColor(string color)
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubColors, new UpdateClubColorsRequest(color, ClubBranding.DefaultSecondaryColor), Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        var club = await Http.GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("primaryColor", problem!.Errors.Keys);
        Assert.Equal(ClubBranding.DefaultPrimaryColor, club!.PrimaryColor);
    }

    [Fact]
    public async Task UpdateColors_BothInvalid_ReportsBothFields()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubColors, new UpdateClubColorsRequest("x", DarkBlue), Ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Contains("primaryColor", problem!.Errors.Keys);
        Assert.Contains("secondaryColor", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadLogo_Png_IsServedOnThePublicSiteWithItsType()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var content = PngHeader.Concat(new byte[] { 1, 2, 3, 4 }).ToArray();

        var response = await UploadAsync(owner.Token, content, "logo.png");
        var publicClub = await Http.WithBearer(null).GetFromJsonAsync<PublicClubDto>($"{Routes.PublicClubs}/{owner.Club!.Slug}", Ct);
        var logo = await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club.Slug), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(publicClub!.LogoVersion);
        Assert.Equal("image/png", logo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(content, await logo.Content.ReadAsByteArrayAsync(Ct));
    }

    [Fact]
    public async Task UploadLogo_Jpeg_IsAccepted()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await UploadAsync(owner.Token, JpegHeader, "logo.jpg");
        var logo = await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club!.Slug), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", logo.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task UploadLogo_NotAnImageEvenIfNamedPng_Returns400()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await UploadAsync(owner.Token, "<svg onload=alert(1)/>"u8.ToArray(), "logo.png", "image/png");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("logo", problem!.Errors.Keys);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club!.Slug), Ct)).StatusCode);
    }

    [Fact]
    public async Task UploadLogo_OverTwoMegabytes_Returns400()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var tooBig = PngHeader.Concat(new byte[LogoRules.MaxBytes]).ToArray();

        var response = await UploadAsync(owner.Token, tooBig, "logo.png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club!.Slug), Ct)).StatusCode);
    }

    [Fact]
    public async Task UploadLogo_ExactlyTwoMegabytes_IsAccepted()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var content = PngHeader.Concat(new byte[LogoRules.MaxBytes - PngHeader.Length]).ToArray();

        var response = await UploadAsync(owner.Token, content, "logo.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadLogo_ANewOneReplacesTheOldAndChangesTheVersion()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        var first = await (await UploadAsync(owner.Token, PngHeader, "a.png")).Content.ReadFromJsonAsync<ClubDto>(Ct);

        var second = await (await UploadAsync(owner.Token, JpegHeader, "b.jpg")).Content.ReadFromJsonAsync<ClubDto>(Ct);
        var logo = await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club!.Slug), Ct);

        Assert.NotEqual(first!.LogoVersion, second!.LogoVersion);
        Assert.Equal("image/jpeg", logo.Content.Headers.ContentType!.MediaType);
        Assert.Single(Directory.GetFiles(StorageFolderOf(owner.Club.Id)));
    }

    [Fact]
    public async Task RemoveLogo_DeletesTheFileAndThePublicSiteHasNone()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);
        await UploadAsync(owner.Token, PngHeader, "logo.png");

        var response = await Http.WithBearer(owner.Token).DeleteAsync(Routes.ClubLogo, Ct);
        var publicClub = await Http.WithBearer(null).GetFromJsonAsync<PublicClubDto>($"{Routes.PublicClubs}/{owner.Club!.Slug}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(publicClub!.LogoVersion);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.WithBearer(null).GetAsync(LogoUrl(owner.Club.Slug), Ct)).StatusCode);
        Assert.Empty(Directory.GetFiles(StorageFolderOf(owner.Club.Id)));
    }

    [Fact]
    public async Task Logo_OfAnotherClubIsNeverServed()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A");
        var b = await Http.RegisterWithClubAsync(Ct, "Club B");
        await UploadAsync(a.Token, PngHeader, "logo.png");

        var response = await Http.WithBearer(null).GetAsync(LogoUrl(b.Club!.Slug), Ct);
        var clubB = await Http.WithBearer(b.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(clubB!.LogoVersion);
    }

    [Fact]
    public async Task Appearance_ChangesOnlyTouchTheClubOfTheSession()
    {
        var a = await Http.RegisterWithClubAsync(Ct, "Club A");
        var b = await Http.RegisterWithClubAsync(Ct, "Club B");

        await Http.WithBearer(a.Token).PutAsJsonAsync(Routes.ClubColors, new UpdateClubColorsRequest(BrightRed, BrightRed), Ct);
        await Http.WithBearer(a.Token).PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest(ClubModules.None), Ct);
        var clubB = await Http.WithBearer(b.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(ClubBranding.DefaultPrimaryColor, clubB!.PrimaryColor);
        Assert.Equal(ClubModules.All, clubB.EnabledModules);
    }

    [Fact]
    public async Task NewClub_StartsWithEverySectionVisible()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var club = await Http.WithBearer(owner.Token).GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(ClubModules.All, club!.EnabledModules);
    }

    [Fact]
    public async Task UpdateModules_IsReadBack()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest(ClubModules.Tournaments), Ct);
        var club = await Http.GetFromJsonAsync<ClubDto>(Routes.CurrentClub, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ClubModules.Tournaments, club!.EnabledModules);
    }

    [Fact]
    public async Task UpdateModules_UnknownSection_Returns400()
    {
        var owner = await Http.RegisterWithClubAsync(Ct);

        var response = await Http.WithBearer(owner.Token).PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest((ClubModules)64), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisabledSection_BlocksItsEndpoints_AndEnablingItOpensThemAgain()
    {
        using var probe = WithProbeController();
        var http = probe.CreateClient();
        var owner = await http.RegisterWithClubAsync(Ct);
        var authorized = http.WithBearer(owner.Token);

        var whileEnabled = await authorized.GetAsync(ModuleProbeController.Route, Ct);
        await authorized.PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest(ClubModules.Tournaments), Ct);
        var whileHidden = await authorized.GetAsync(ModuleProbeController.Route, Ct);
        await authorized.PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest(ClubModules.All), Ct);
        var afterEnabling = await authorized.GetAsync(ModuleProbeController.Route, Ct);

        Assert.Equal(HttpStatusCode.OK, whileEnabled.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, whileHidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterEnabling.StatusCode);
    }

    [Fact]
    public async Task DisabledSection_OfAnotherClubDoesNotBlockMine()
    {
        using var probe = WithProbeController();
        var http = probe.CreateClient();
        var a = await http.RegisterWithClubAsync(Ct, "Club A");
        var b = await http.RegisterWithClubAsync(Ct, "Club B");

        await http.WithBearer(a.Token).PutAsJsonAsync(Routes.ClubModules, new UpdateClubModulesRequest(ClubModules.None), Ct);

        Assert.Equal(HttpStatusCode.NotFound, (await http.WithBearer(a.Token).GetAsync(ModuleProbeController.Route, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.WithBearer(b.Token).GetAsync(ModuleProbeController.Route, Ct)).StatusCode);
    }

    private WebApplicationFactory<Program> WithProbeController() =>
        Api.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ModuleProbeController).Assembly)));

    private static string LogoUrl(string slug) => $"{Routes.PublicClubs}/{slug}/logo";

    private string StorageFolderOf(Guid clubId)
    {
        var root = Api.Services.GetRequiredService<IConfiguration>()["Storage:RootPath"]!;
        return Path.Combine(root, "logos", clubId.ToString("N"));
    }

    private async Task<HttpResponseMessage> UploadAsync(string token, byte[] content, string fileName, string contentType = "image/png")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await Http.WithBearer(token).PutAsync(Routes.ClubLogo, form, Ct);
    }
}
