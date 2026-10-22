using System.Net;
using System.Text.RegularExpressions;

namespace ConCancha.Infrastructure.Email;

/// <summary>Wraps a plain-text body in the shared ConCancha layout so every message looks the same in mail clients.</summary>
public static partial class EmailHtmlLayout
{
    private const string Footer = "Este mail lo mandó ConCancha. Si no esperabas recibirlo, podés ignorarlo.";
    private const string LineBreak = "<br>";

    public static string Render(string subject, string body) =>
        $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(subject)}</title></head>
        <body style="margin:0;padding:24px;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#1f2933;">
        <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;padding:24px;">
        <p style="margin:0 0 16px;font-size:20px;font-weight:bold;">ConCancha</p>
        <p style="margin:0;font-size:15px;line-height:1.5;">{RenderParagraphs(body)}</p>
        <p style="margin:24px 0 0;font-size:12px;color:#6b7280;">{Footer}</p>
        </div>
        </body>
        </html>
        """;

    private static string RenderParagraphs(string body)
    {
        var encoded = WebUtility.HtmlEncode(body.ReplaceLineEndings("\n"));
        var linked = Link().Replace(encoded, match => $"<a href=\"{match.Value}\" style=\"color:#15803d;\">{match.Value}</a>");
        return linked.Replace("\n", LineBreak);
    }

    [GeneratedRegex(@"https?://[^\s<]+")]
    private static partial Regex Link();
}
