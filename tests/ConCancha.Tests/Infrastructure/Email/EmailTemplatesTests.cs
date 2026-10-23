using ConCancha.Application.Emails;
using ConCancha.Infrastructure.Email;

namespace ConCancha.Tests.Infrastructure.Email;

public class EmailTemplatesTests
{
    private const string SampleLink = "https://concancha.test/panel/restablecer?mail=ana%40club.com&token=abc";

    [Fact]
    public void PasswordReset_IsInSpanishAndCarriesTheLinkOnItsOwnLine()
    {
        var email = EmailTemplates.PasswordReset(SampleLink);

        Assert.Equal("Restablecé tu contraseña de ConCancha", email.Subject);
        Assert.Equal(SampleLink, email.Body.Split('\n')[1]);
        Assert.Contains("Si no lo pediste vos", email.Body);
    }

    [Fact]
    public void TeamInvitation_NamesTheClubAndCarriesTheLinkOnItsOwnLine()
    {
        var email = EmailTemplates.TeamInvitation("Club Sur", SampleLink, validDays: 7);

        Assert.Contains("Club Sur", email.Subject);
        Assert.Equal(SampleLink, email.Body.Split('\n')[1]);
        Assert.Contains("7 días", email.Body);
    }

    [Fact]
    public void TeamMemberAdded_PointsToThePanel()
    {
        var email = EmailTemplates.TeamMemberAdded("Club Sur", "https://concancha.test/panel");

        Assert.Contains("Club Sur", email.Subject);
        Assert.Contains("https://concancha.test/panel", email.Body);
    }

    [Fact]
    public void HtmlLayout_MakesTheLinkClickableAndKeepsLineBreaks()
    {
        var email = EmailTemplates.PasswordReset(SampleLink);

        var html = EmailHtmlLayout.Render(email.Subject, email.Body);

        Assert.Contains("<html lang=\"es\">", html);
        Assert.Contains($"<a href=\"{SampleLink.Replace("&", "&amp;")}\"", html);
        Assert.Contains("<br>", html);
        Assert.Contains("Este mail lo mandó ConCancha", html);
    }

    [Fact]
    public void HtmlLayout_EncodesMarkupFromTheBody()
    {
        var html = EmailHtmlLayout.Render("Hola", "Club <script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Theory]
    [InlineData("ana@club.com", "a***@club.com")]
    [InlineData("sin-arroba", "***")]
    [InlineData("@club.com", "***")]
    public void RecipientMask_HidesTheMailbox(string address, string expected) =>
        Assert.Equal(expected, EmailRecipientMask.Mask(address));

    [Theory]
    [InlineData(EmailProvider.Log, "", "", true)]
    [InlineData(EmailProvider.Resend, "ConCancha <no-responder@club.com>", "re_key", true)]
    [InlineData(EmailProvider.Resend, "", "re_key", false)]
    [InlineData(EmailProvider.Resend, "ConCancha <no-responder@club.com>", "", false)]
    public void Options_RequireSenderAndKeyForTheRealProvider(EmailProvider provider, string from, string key, bool complete) =>
        Assert.Equal(complete, new EmailOptions { Provider = provider, From = from, ResendApiKey = key }.IsComplete);
}
