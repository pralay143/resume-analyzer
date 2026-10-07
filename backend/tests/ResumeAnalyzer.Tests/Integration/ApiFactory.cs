using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ResumeAnalyzer.Api.Data;
using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Tests.Integration;

/// <summary>
/// Runs the real API against a throwaway PostgreSQL database (created from the migrations, dropped afterwards)
/// with Claude replaced by <see cref="FakeAiAnalysisService"/>.
/// </summary>
/// <remarks>
/// The server comes from the TEST_DATABASE_CONNECTION environment variable, or else from the API's local
/// appsettings.Development.json. Only the database name is replaced, so no credentials live in the tests.
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ConnectionEnvironmentVariable = "TEST_DATABASE_CONNECTION";

    private readonly string _connectionString;

    public ApiFactory()
    {
        var builder = new NpgsqlConnectionStringBuilder(ResolveServerConnectionString())
        {
            Database = $"resume_analyzer_test_{Guid.NewGuid():N}"
        };
        _connectionString = builder.ConnectionString;

        // Program.cs reads the connection string before the host is built, which is too early for
        // WebApplicationFactory's configuration hooks, so it has to come from the environment.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _connectionString);
    }

    public FakeAiAnalysisService FakeAi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Gemini:ApiKey", "test-key-not-used");
        builder.UseSetting("Anthropic:ApiKey", "test-key-not-used");
        builder.UseSetting("RateLimiting:Analyses:PermitLimit", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAiAnalysisService>();
            services.AddSingleton<IAiAnalysisService>(FakeAi);
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }

    /// <summary>Empties the database and resets the fake AI, so each test starts clean.</summary>
    public async Task ResetAsync()
    {
        FakeAi.Reset();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Analyses.ExecuteDeleteAsync();
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static string ResolveServerConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var settingsPath = Path.Combine(dir.FullName, "src", "ResumeAnalyzer.Api", "appsettings.Development.json");
            if (!File.Exists(settingsPath))
            {
                continue;
            }

            using var json = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (json.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
                connectionStrings.TryGetProperty("Default", out var value) &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!;
            }
        }

        throw new InvalidOperationException(
            "Integration tests need PostgreSQL. Set the " + ConnectionEnvironmentVariable + " environment variable " +
            "or ConnectionStrings:Default in backend/src/ResumeAnalyzer.Api/appsettings.Development.json.");
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "API";
}
