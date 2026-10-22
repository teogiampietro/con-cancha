using ConCancha.Infrastructure.Persistence;
using Npgsql;

namespace ConCancha.Tests.Infrastructure;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_converts_postgres_uri_to_npgsql_format()
    {
        var result = new NpgsqlConnectionStringBuilder(
            PostgresConnectionString.Normalize("postgres://user:p%40ss@dpg-abc:5433/concancha"));

        Assert.Equal("dpg-abc", result.Host);
        Assert.Equal(5433, result.Port);
        Assert.Equal("concancha", result.Database);
        Assert.Equal("user", result.Username);
        Assert.Equal("p@ss", result.Password);
    }

    [Fact]
    public void Normalize_keeps_npgsql_format_untouched()
    {
        const string original = "Host=localhost;Port=5432;Database=concancha";

        Assert.Equal(original, PostgresConnectionString.Normalize(original));
    }
}
