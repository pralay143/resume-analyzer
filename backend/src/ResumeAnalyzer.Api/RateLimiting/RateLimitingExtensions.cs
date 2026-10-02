using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ResumeAnalyzer.Api.RateLimiting;

public class AnalysisRateLimitOptions
{
    public const string SectionName = "RateLimiting:Analyses";

    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 24 * 60)]
    public int WindowMinutes { get; set; } = 60;
}

public static class RateLimitingExtensions
{
    /// <summary>Policy for endpoints that call the paid AI API: a fixed window per client IP.</summary>
    public const string AnalysesPolicy = "analyses";

    public static IServiceCollection AddAnalysisRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<AnalysisRateLimitOptions>()
            .BindConfiguration(AnalysisRateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AnalysesPolicy, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<AnalysisRateLimitOptions>>().Value;
                // With ForwardedHeaders this is the real client IP, not the proxy's.
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromMinutes(limits.WindowMinutes),
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, ct) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;

                var detail = "You've reached the limit for new analyses. Please try again later.";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                    response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                    detail = $"You've reached the limit for new analyses. Please try again in {FormatWait(seconds)}.";
                }

                var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = detail
                    }
                });
            };
        });

        return services;
    }

    private static string FormatWait(int seconds) => seconds switch
    {
        < 60 => $"{seconds} seconds",
        < 120 => "1 minute",
        _ => $"{(int)Math.Ceiling(seconds / 60.0)} minutes"
    };
}
