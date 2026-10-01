using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Api.Data;
using ResumeAnalyzer.Api.Middleware;
using ResumeAnalyzer.Api.Services;
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
            .AllowAnyMethod());
});

builder.Services.AddSingleton<IResumeParser, PdfResumeParser>();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
