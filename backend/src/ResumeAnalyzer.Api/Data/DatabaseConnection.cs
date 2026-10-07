using Npgsql;

namespace ResumeAnalyzer.Api.Data;

public static class DatabaseConnection
{
    public const string UrlVariable = "DATABASE_URL";

    /// <summary>
    /// Returns the connection string from ConnectionStrings:Default, or else converts DATABASE_URL
    /// (the postgresql://user:password@host:port/database form that Neon and Render provide).
    /// </summary>
    /// <returns>Null when neither is set.</returns>
    public static string? Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var url = configuration[UrlVariable];
        return string.IsNullOrWhiteSpace(url) ? null : FromUrl(url);
    }

    /// <summary>Converts a postgres:// or postgresql:// URL to an Npgsql connection string.</summary>
    /// <exception cref="FormatException">The URL isn't a PostgreSQL URL.</exception>
    public static string FromUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
        {
            // The URL itself isn't included: it contains the password.
            throw new FormatException($"{UrlVariable} must be a postgresql://user:password@host:port/database URL.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            // Hosted databases are reached over the internet, so encryption is the default.
            SslMode = SslMode.Require,
            // Hosted Postgres (Neon, Render) uses TLS, not Kerberos. Skipping the GSS probe avoids a
            // "Cannot load library libgssapi_krb5" error on slim container images.
            GssEncryptionMode = GssEncryptionMode.Disable
        };

        foreach (var (key, value) in ParseQuery(uri.Query))
        {
            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(value.Replace("-", string.Empty), ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(value, ignoreCase: true);
                    break;
                // Other libpq options have no Npgsql equivalent that matters here.
            }
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Select(parts => (Uri.UnescapeDataString(parts[0]), parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty));
}
