using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ResumeAnalyzer.Api.Services.Ai;

public static class AiServiceCollectionExtensions
{
    private const string AnthropicBaseUrl = "https://api.anthropic.com/v1/";
    private const string AnthropicVersion = "2023-06-01";
    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/";

    // 529 is Anthropic's "overloaded" status.
    private static readonly HashSet<int> ClaudeRetryableStatusCodes =
        [(int)HttpStatusCode.TooManyRequests, (int)HttpStatusCode.InternalServerError, 529];

    // Gemini reports overload as 503 UNAVAILABLE.
    private static readonly HashSet<int> GeminiRetryableStatusCodes =
        [(int)HttpStatusCode.TooManyRequests, (int)HttpStatusCode.InternalServerError, (int)HttpStatusCode.ServiceUnavailable];

    /// <summary>
    /// Registers the <see cref="IAiAnalysisService"/> chosen by "Ai:Provider". Only that provider's options,
    /// including its API key, are validated at startup.
    /// </summary>
    public static IHttpClientBuilder AddAiAnalysis(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>()?.Provider ?? AiProvider.Gemini;
        services.Configure<AiOptions>(o => o.Provider = provider);

        return provider switch
        {
            AiProvider.Gemini => services.AddGeminiAnalysis(),
            AiProvider.Claude => services.AddClaudeAnalysis(),
            _ => throw new InvalidOperationException($"Unsupported Ai:Provider '{provider}'. Use 'Gemini' or 'Claude'.")
        };
    }

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

        clientBuilder.AddAiResilience(
            sp => sp.GetRequiredService<IOptions<AnthropicOptions>>().Value.TimeoutSeconds,
            ClaudeRetryableStatusCodes);

        return clientBuilder;
    }

    /// <summary>
    /// Registers <see cref="IAiAnalysisService"/> with a resilient typed HttpClient for the Gemini generateContent API.
    /// </summary>
    public static IHttpClientBuilder AddGeminiAnalysis(this IServiceCollection services)
    {
        services.AddOptions<GeminiOptions>()
            .BindConfiguration(GeminiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var clientBuilder = services.AddHttpClient<IAiAnalysisService, GeminiAnalysisService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(GeminiBaseUrl);
            // A header rather than the ?key= query parameter, so the key never appears in logged URLs.
            client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        clientBuilder.AddAiResilience(
            sp => sp.GetRequiredService<IOptions<GeminiOptions>>().Value.TimeoutSeconds,
            GeminiRetryableStatusCodes);

        return clientBuilder;
    }

    private static void AddAiResilience(
        this IHttpClientBuilder clientBuilder,
        Func<IServiceProvider, int> timeoutSeconds,
        IReadOnlySet<int> retryableStatusCodes)
    {
        clientBuilder.AddStandardResilienceHandler().Configure((resilience, sp) =>
        {
            var attemptTimeout = TimeSpan.FromSeconds(timeoutSeconds(sp));

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
                { Result: { } response } => retryableStatusCodes.Contains((int)response.StatusCode),
                _ => false
            });
        });
    }
}
