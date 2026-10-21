using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConCancha.Infrastructure.Persistence;

/// <summary>Only for <c>dotnet ef</c> (migrations). At runtime the connection comes from configuration.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConCanchaDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Default";
    private const string LocalConnectionString = "Host=localhost;Port=5432;Database=concancha;Username=concancha;Password=concancha";

    public ConCanchaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? LocalConnectionString;
        var options = new DbContextOptionsBuilder<ConCanchaDbContext>().UseNpgsql(connectionString).Options;
        return new ConCanchaDbContext(options);
    }
}
