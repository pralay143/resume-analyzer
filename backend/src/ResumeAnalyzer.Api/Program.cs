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

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:Default' is missing. " +
        "Copy appsettings.Development.example.json to appsettings.Development.json and set it, " +
        "or set the ConnectionStrings__Default environment variable.");
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
builder.Services.AddClaudeAnalysis();
builder.Services.AddAnalysisRateLimiting();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the integration tests reference the app with WebApplicationFactory<Program>.
public partial class Program;
