using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ResumeAnalyzer.Api.Services.Ai;

public static class AiServiceCollectionExtensions
{
    private const string AnthropicBaseUrl = "https://api.anthropic.com/v1/";
    private const string AnthropicVersion = "2023-06-01";

    // 529 is Anthropic's "overloaded" status.
    private static readonly HashSet<int> RetryableStatusCodes =
        [(int)HttpStatusCode.TooManyRequests, (int)HttpStatusCode.InternalServerError, 529];

    /// <summary>
    /// Registers <see cref="IAiAnalysisService"/> with a resilient typed HttpClient for the Anthropic Messages API.
    /// </summary>
    public static IHttpClientBuilder AddClaudeAnalysis(this IServiceCollection services)
    {
        services.AddOptions<AnthropicOptions>()
            .BindConfiguration(AnthropicOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var clientBuilder = services.AddHttpClient<IAiAnalysisService, ClaudeAnalysisService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            client.BaseAddress = new Uri(AnthropicBaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
            // The resilience handler's total timeout governs instead.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        clientBuilder.AddStandardResilienceHandler().Configure((resilience, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            var attemptTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            resilience.AttemptTimeout.Timeout = attemptTimeout;
            resilience.TotalRequestTimeout.Timeout = attemptTimeout * 2 + TimeSpan.FromSeconds(30);
            // The standard handler requires the sampling window to be at least twice the attempt timeout.
            resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2;

            resilience.Retry.MaxRetryAttempts = 3;
            resilience.Retry.BackoffType = DelayBackoffType.Exponential;
            resilience.Retry.UseJitter = true;
            resilience.Retry.Delay = TimeSpan.FromSeconds(2);
            // Retry rate limits, server errors and overload, plus network failures. A timed-out attempt isn't
            // retried: waiting another 90 seconds is rarely worth it for an interactive request.
            resilience.Retry.ShouldHandle = args => ValueTask.FromResult(args.Outcome switch
            {
                { Exception: HttpRequestException } => true,
                { Result: { } response } => RetryableStatusCodes.Contains((int)response.StatusCode),
                _ => false
            });
        });

        return clientBuilder;
    }
}
