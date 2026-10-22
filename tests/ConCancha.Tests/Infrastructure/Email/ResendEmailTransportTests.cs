using System.Net;
using System.Text.Json;
using ConCancha.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace ConCancha.Tests.Infrastructure.Email;

public class ResendEmailTransportTests
{
    private const string Sender = "ConCancha <no-responder@club.com>";
    private const string ApiKey = "re_test_key";
    private static readonly OutgoingEmail Sample = new("ana@club.com", "Asunto", "Hola\nhttps://club.test/x");

    [Fact]
    public async Task Deliver_PostsTheMessageWithTheApiKey()
    {
        var handler = new StubHandler(HttpStatusCode.OK);

        await CreateTransport(handler).DeliverAsync(Sample, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(new Uri(ResendEmailTransport.ApiUrl + "emails"), handler.Request.RequestUri);
        Assert.Equal($"Bearer {ApiKey}", handler.Request.Headers.Authorization!.ToString());
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal(Sender, json.RootElement.GetProperty("from").GetString());
        Assert.Equal(Sample.To, json.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal(Sample.Body, json.RootElement.GetProperty("text").GetString());
        Assert.Contains("<a href=", json.RootElement.GetProperty("html").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    public async Task Deliver_TellsTransientFromPermanentFailures(HttpStatusCode status, bool transient)
    {
        var failure = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => CreateTransport(new StubHandler(status)).DeliverAsync(Sample, CancellationToken.None));

        Assert.Equal(transient, failure.IsTransient);
    }

    [Fact]
    public async Task Deliver_TreatsAnUnreachableProviderAsTransient()
    {
        var failure = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => CreateTransport(new StubHandler(new HttpRequestException("down"))).DeliverAsync(Sample, CancellationToken.None));

        Assert.True(failure.IsTransient);
    }

    private static ResendEmailTransport CreateTransport(StubHandler handler) =>
        new(new StubClientFactory(handler), Options.Create(new EmailOptions { From = Sender, ResendApiKey = ApiKey }));

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri(ResendEmailTransport.ApiUrl) };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly Exception? _failure;

        public StubHandler(HttpStatusCode status) => _status = status;

        public StubHandler(Exception failure) => _failure = failure;

        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_failure is not null) throw _failure;
            Request = request;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status);
        }
    }
}
