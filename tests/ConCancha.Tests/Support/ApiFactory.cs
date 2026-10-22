using ConCancha.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConCancha.Tests.Support;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "clave-de-pruebas-0123456789-0123456789-0123456789";
    public const string WebBaseUrl = "http://web.test";
    private const string UnlimitedRequests = "100000";

    private readonly TestDatabase _database = new();
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"concancha-tests-{Guid.NewGuid():N}");

    public CapturingEmailSender Emails { get; } = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
        if (Directory.Exists(_storageRoot)) Directory.Delete(_storageRoot, recursive: true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(TestSettings()));
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IEmailSender>(Emails)));
    }

    private Dictionary<string, string?> TestSettings() => new()
    {
        ["ConnectionStrings:Default"] = _database.ConnectionString,
        ["Jwt:Key"] = JwtKey,
        ["Database:MigrateOnStartup"] = "true",
        ["Auth:CheckLeakedPasswords"] = "false",
        ["RateLimit:AuthPerMinute"] = UnlimitedRequests,
        ["RateLimit:QueryPerMinute"] = UnlimitedRequests,
        ["RateLimit:PublicBookingPerMinute"] = UnlimitedRequests,
        ["Web:BaseUrl"] = WebBaseUrl,
        ["Storage:RootPath"] = _storageRoot,
        ["Email:Provider"] = "Log",
    };
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
