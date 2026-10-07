using Microsoft.Extensions.Configuration;
using Npgsql;
using ResumeAnalyzer.Api.Data;

namespace ResumeAnalyzer.Tests.Data;

public class DatabaseConnectionTests
{
    [Fact]
    public void FromUrl_NeonStyleUrl_MapsEveryPart()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgresql://app_user:s3cr%40t@ep-cool-name-123.eu-central-1.aws.neon.tech/resume_db?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-name-123.eu-central-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("resume_db", result.Database);
        Assert.Equal("app_user", result.Username);
        Assert.Equal("s3cr@t", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
        Assert.Equal(GssEncryptionMode.Disable, result.GssEncryptionMode);
    }

    [Fact]
    public void FromUrl_RenderStyleUrlWithPort_DefaultsToRequiredSsl()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgres://render_user:pw@dpg-abc123.oregon-postgres.render.com:6543/render_db"));

        Assert.Equal(6543, result.Port);
        Assert.Equal("render_db", result.Database);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void FromUrl_SslModeWithDash_IsParsed()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgresql://u:p@localhost/db?sslmode=verify-full"));

        Assert.Equal(SslMode.VerifyFull, result.SslMode);
    }

    [Theory]
    [InlineData("mysql://u:p@host/db")]
    [InlineData("not a url")]
    public void FromUrl_NotPostgres_ThrowsWithoutLeakingTheUrl(string url)
    {
        var ex = Assert.Throws<FormatException>(() => DatabaseConnection.FromUrl(url));

        Assert.DoesNotContain("p@host", ex.Message);
    }

    [Fact]
    public void Resolve_PrefersConnectionStringOverDatabaseUrl()
    {
        var configuration = Configuration(new()
        {
            ["ConnectionStrings:Default"] = "Host=explicit;Database=a",
            ["DATABASE_URL"] = "postgresql://u:p@from-url/b"
        });

        Assert.Equal("Host=explicit;Database=a", DatabaseConnection.Resolve(configuration));
    }

    [Fact]
    public void Resolve_FallsBackToDatabaseUrl()
    {
        var configuration = Configuration(new() { ["DATABASE_URL"] = "postgresql://u:p@from-url/b" });

        Assert.Equal("from-url", new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration)).Host);
    }

    [Fact]
    public void Resolve_NothingConfigured_ReturnsNull()
    {
        Assert.Null(DatabaseConnection.Resolve(Configuration(new())));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
