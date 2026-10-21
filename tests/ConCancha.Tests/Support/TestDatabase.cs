using Npgsql;
using Testcontainers.PostgreSql;

namespace ConCancha.Tests.Support;

/// <summary>
/// Real PostgreSQL for tests. By default starts a container (Docker required).
/// With the <c>TEST_POSTGRES</c> variable (connection string of a running server) it creates a throwaway database there.
/// </summary>
public sealed class TestDatabase : IAsyncLifetime
{
    private const string ExternalServerVariable = "TEST_POSTGRES";
    private const string PostgresImage = "postgres:17";

    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(ExternalServerVariable);
        if (string.IsNullOrWhiteSpace(external))
            await StartContainerAsync();
        else
            await CreateExternalDatabaseAsync(external);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
        if (_adminConnectionString is not null && _databaseName is not null)
            await DropExternalDatabaseAsync(_adminConnectionString, _databaseName);
    }

    private async Task StartContainerAsync()
    {
        _container = new PostgreSqlBuilder(PostgresImage).Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    private async Task CreateExternalDatabaseAsync(string adminConnectionString)
    {
        _adminConnectionString = adminConnectionString;
        _databaseName = "test_" + Guid.NewGuid().ToString("N");
        await ExecuteAsync(adminConnectionString, $"CREATE DATABASE \"{_databaseName}\"");
        ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = _databaseName }.ConnectionString;
    }

    private static async Task DropExternalDatabaseAsync(string adminConnectionString, string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteAsync(adminConnectionString, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
