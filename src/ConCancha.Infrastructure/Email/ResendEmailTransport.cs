using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure.Email;

/// <summary>Sends through the Resend HTTP API, since the hosting free tier blocks outbound SMTP ports.</summary>
public sealed class ResendEmailTransport(IHttpClientFactory httpClientFactory, IOptions<EmailOptions> options) : IEmailTransport
{
    public const string HttpClientName = "resend";
    public const string ApiUrl = "https://api.resend.com/";

    private const string SendPath = "emails";
    private const string BearerScheme = "Bearer";
    private const int FirstServerErrorCode = 500;

    public async Task DeliverAsync(OutgoingEmail email, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SendPath) { Content = JsonContent.Create(ToPayload(email)) };
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, options.Value.ResendApiKey);

        using var response = await SendOrThrowTransientAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return;

        throw new EmailDeliveryException($"Resend answered {(int)response.StatusCode}.", IsTransient(response.StatusCode));
    }

    private async Task<HttpResponseMessage> SendOrThrowTransientAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        try
        {
            return await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException
                                              || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new EmailDeliveryException("Resend could not be reached.", isTransient: true, exception);
        }
    }

    private ResendPayload ToPayload(OutgoingEmail email) =>
        new(options.Value.From, [email.To], email.Subject, email.Body, EmailHtmlLayout.Render(email.Subject, email.Body));

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= FirstServerErrorCode;

    private sealed record ResendPayload(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("html")] string Html);
}
