using Npgsql;

namespace ConCancha.Infrastructure.Persistence;

public static class PostgresConnectionString
{
    private const string PostgresScheme = "postgres";
    private const string PostgresqlScheme = "postgresql";
    private const char UserInfoSeparator = ':';

    public static string Normalize(string connectionString)
    {
        if (!TryParseUri(connectionString, out var uri))
            return connectionString;

        var credentials = uri.UserInfo.Split(UserInfoSeparator, 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };
        if (!uri.IsDefaultPort)
            builder.Port = uri.Port;

        return builder.ConnectionString;
    }

    private static bool TryParseUri(string connectionString, out Uri uri) =>
        Uri.TryCreate(connectionString, UriKind.Absolute, out uri!)
        && (uri.Scheme == PostgresScheme || uri.Scheme == PostgresqlScheme);
}
