using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Api.Data;
using ResumeAnalyzer.Api.Middleware;
using ResumeAnalyzer.Api.RateLimiting;
using ResumeAnalyzer.Api.Services;
using ResumeAnalyzer.Api.Services.Ai;
using Scalar.AspNetCore;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Hosts like Render tell the app which port to listen on. Locally PORT is unset and launchSettings applies.
if (builder.Configuration["PORT"] is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var connectionString = DatabaseConnection.Resolve(builder.Configuration);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection is configured. Locally, copy appsettings.Development.example.json to " +
        "appsettings.Development.json and set ConnectionStrings:Default. In production, set the " +
        "DATABASE_URL (postgresql://...) or ConnectionStrings__Default environment variable.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location", "Retry-After"));
});

// Render (and most hosts) put the API behind a proxy. Trust the one X-Forwarded-For entry that proxy appends,
// so rate limiting sees the real client IP. The proxy's address isn't known in advance, hence the cleared lists.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<IResumeParser, PdfResumeParser>();
builder.Services.AddSingleton<IMatchScoringService, MatchScoringService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddAiAnalysis(builder.Configuration);
builder.Services.AddAnalysisRateLimiting();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await MigrateDatabaseAsync(app);
}

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Applies pending EF Core migrations. Safe while a single instance runs, as on Render's free tier.
static async Task MigrateDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count == 0)
    {
        app.Logger.LogInformation("Database is up to date; no migrations to apply");
        return;
    }

    app.Logger.LogInformation("Applying {Count} database migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied");
}

// Lets the integration tests reference the app with WebApplicationFactory<Program>.
public partial class Program;
