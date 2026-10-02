using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ResumeAnalyzer.Api.Services.Ai;

public class GeminiAnalysisService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiAnalysisService> logger) : IAiAnalysisService
{
    private const string GenericFailureMessage =
        "The AI analysis service is unavailable right now. Please try again in a few minutes.";

    private const string UnexpectedResponseMessage = "The AI returned an unexpected response. Please try again.";

    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly GeminiOptions _options = options.Value;

    public async Task<AiAnalysisResult> AnalyzeAsync(string resumeText, string jobDescription, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resumeText);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobDescription);

        var request = new GenerateContentRequest(
            SystemInstruction: new GeminiContent([new GeminiPart(AnalysisPrompt.GeminiSystem)]),
            Contents: [new GeminiContent([new GeminiPart(AnalysisPrompt.BuildUserMessage(resumeText, jobDescription))], "user")],
            GenerationConfig: new GeminiGenerationConfig(
                Temperature: 0,
                MaxOutputTokens: _options.MaxOutputTokens,
                ResponseMimeType: "application/json",
                ResponseJsonSchema: AnalysisPrompt.ToolInputSchema,
                ThinkingConfig: string.IsNullOrWhiteSpace(_options.ThinkingLevel)
                    ? null
                    : new GeminiThinkingConfig(_options.ThinkingLevel)));

        var response = await SendAsync(request, ct);

        var candidate = response.Candidates?.FirstOrDefault();
        if (candidate is null)
        {
            logger.LogWarning("Gemini response had no candidates (block reason: {BlockReason})",
                response.PromptFeedback?.BlockReason);
            throw new AiAnalysisException(UnexpectedResponseMessage);
        }

        if (candidate.FinishReason == "MAX_TOKENS")
        {
            logger.LogWarning(
                "Gemini response hit the {MaxOutputTokens} maxOutputTokens limit ({OutputTokens} output, {ThoughtTokens} thinking tokens)",
                _options.MaxOutputTokens, response.UsageMetadata?.CandidatesTokenCount, response.UsageMetadata?.ThoughtsTokenCount);
            throw new AiAnalysisException(
                "The AI response was cut off before it finished. Try a shorter job description.");
        }

        if (candidate.FinishReason is not (null or "STOP"))
        {
            logger.LogWarning("Gemini stopped with finish reason {FinishReason}", candidate.FinishReason);
            throw new AiAnalysisException(UnexpectedResponseMessage);
        }

        var input = ParseAnalysis(candidate);
        var model = response.ModelVersion ?? _options.Model;
        var inputTokens = response.UsageMetadata?.PromptTokenCount ?? 0;
        // Gemini bills thinking tokens as output, so they're included here.
        var outputTokens = (response.UsageMetadata?.CandidatesTokenCount ?? 0) + (response.UsageMetadata?.ThoughtsTokenCount ?? 0);

        logger.LogInformation(
            "Gemini analysis completed with {Model}: {InputTokens} input tokens, {OutputTokens} output tokens",
            model, inputTokens, outputTokens);

        return AnalysisResultMapper.ToResult(input, model, inputTokens, outputTokens);
    }

    private async Task<GenerateContentResponse> SendAsync(GenerateContentRequest request, CancellationToken ct)
    {
        var path = $"models/{Uri.EscapeDataString(_options.Model)}:generateContent";

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsJsonAsync(path, request, ApiJsonOptions, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or TaskCanceledException)
        {
            logger.LogWarning("Gemini request timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);
            throw new AiAnalysisException("The AI analysis took too long. Please try again.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Gemini request failed");
            throw new AiAnalysisException(GenericFailureMessage, ex);
        }

        using (httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await ReadAndLogErrorAsync(httpResponse, ct);
                throw new AiAnalysisException(httpResponse.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests =>
                        "The free AI quota is used up for now. Please try again in a minute.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "The AI service rejected the server's credentials. Please contact the site owner.",
                    // Gemini reports an invalid key as 400 INVALID_ARGUMENT.
                    HttpStatusCode.BadRequest when error?.Message?.Contains("API key", StringComparison.OrdinalIgnoreCase) == true =>
                        "The AI service rejected the server's credentials. Please contact the site owner.",
                    _ => GenericFailureMessage
                });
            }

            try
            {
                return await httpResponse.Content.ReadFromJsonAsync<GenerateContentResponse>(ApiJsonOptions, ct)
                       ?? throw new JsonException("Empty response body.");
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Gemini response body could not be parsed");
                throw new AiAnalysisException(UnexpectedResponseMessage, ex);
            }
        }
    }

    // Logs the provider's error status and message only. Request content (resume, job description) is never logged.
    private async Task<GeminiError?> ReadAndLogErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        GeminiError? error = null;
        try
        {
            error = (await response.Content.ReadFromJsonAsync<GeminiErrorResponse>(ApiJsonOptions, ct))?.Error;
        }
        catch (JsonException)
        {
            // Not a JSON error body; the status code is still logged below.
        }

        logger.LogWarning("Gemini API returned {StatusCode}: {ErrorStatus} {ErrorMessage}",
            (int)response.StatusCode, error?.Status, error?.Message);
        return error;
    }

    private ReportAnalysisInput ParseAnalysis(GeminiCandidate candidate)
    {
        // The JSON answer can be split across several parts; thought summaries, if any, are skipped.
        var json = string.Concat((candidate.Content?.Parts ?? [])
            .Where(part => part.Thought != true)
            .Select(part => part.Text));

        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonException("Response contained no text.");
            }

            using var document = JsonDocument.Parse(json);
            return AnalysisResultMapper.Parse(document.RootElement);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Gemini analysis JSON was malformed: {Reason}", ex.Message);
            throw new AiAnalysisException("The AI returned an incomplete analysis. Please try again.", ex);
        }
    }
}
