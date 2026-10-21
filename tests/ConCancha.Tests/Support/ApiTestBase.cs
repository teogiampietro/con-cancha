namespace ConCancha.Tests.Support;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFactory api)
{
    protected ApiFactory Api { get; } = api;

    protected HttpClient Http { get; } = api.CreateClient();

    // xunit 2.x exposes no per-test token, so tests use one that is never cancelled.
    protected static CancellationToken Ct => CancellationToken.None;
}
