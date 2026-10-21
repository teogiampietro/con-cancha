using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Api.Extensions;

public static class WebApplicationExtensions
{
    private const string MigrateOnStartupKey = "Database:MigrateOnStartup";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<ConCanchaDbContext>("postgres", tags: [ReadyTag]);
        return services;
    }

    public static async Task MigrateDatabaseIfEnabledAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue(MigrateOnStartupKey, false)) return;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>/health: the app is alive. /health/ready: the database answers too.</summary>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new() { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }
}
