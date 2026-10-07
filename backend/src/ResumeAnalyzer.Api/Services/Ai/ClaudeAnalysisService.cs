using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ResumeAnalyzer.Api.Services.Ai;

public class ClaudeAnalysisService(
    HttpClient httpClient,
    IOptions<AnthropicOptions> options,
    ILogger<ClaudeAnalysisService> logger) : IAiAnalysisService
{
    private const string GenericFailureMessage =
        "The AI analysis service is unavailable right now. Please try again in a few minutes.";

    private static readonly JsonSerializerOptions ApiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly AnthropicOptions _options = options.Value;

    public async Task<AiAnalysisResult> AnalyzeAsync(string resumeText, string jobDescription, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resumeText);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobDescription);

        var request = new MessagesRequest(
            Model: _options.Model,
            MaxTokens: _options.MaxTokens,
            Temperature: 0,
            System: AnalysisPrompt.ClaudeSystem,
            Tools: [new ToolDefinition(AnalysisPrompt.ToolName, AnalysisPrompt.ToolDescription, AnalysisPrompt.ToolInputSchema)],
            ToolChoice: new ToolChoice("tool", AnalysisPrompt.ToolName),
            Messages: [new RequestMessage("user", AnalysisPrompt.BuildUserMessage(resumeText, jobDescription))]);

        var response = await SendAsync(request, ct);

        if (response.StopReason == "max_tokens")
        {
            logger.LogWarning(
                "Claude response hit the {MaxTokens} max_tokens limit ({OutputTokens} output tokens)",
                _options.MaxTokens, response.Usage?.OutputTokens);
            throw new AiAnalysisException(
                "The AI response was cut off before it finished. Try a shorter job description.");
        }

        var toolUse = response.Content?.FirstOrDefault(block =>
            block.Type == "tool_use" && block.Name == AnalysisPrompt.ToolName);
        if (toolUse is null)
        {
            logger.LogWarning("Claude response had no {ToolName} tool_use block (stop_reason: {StopReason})",
                AnalysisPrompt.ToolName, response.StopReason);
            throw new AiAnalysisException("The AI returned an unexpected response. Please try again.");
        }

        var input = DeserializeToolInput(toolUse.Input);
        var model = response.Model ?? _options.Model;
        var inputTokens = response.Usage?.InputTokens ?? 0;
        var outputTokens = response.Usage?.OutputTokens ?? 0;

        logger.LogInformation(
            "Claude analysis completed with {Model}: {InputTokens} input tokens, {OutputTokens} output tokens",
            model, inputTokens, outputTokens);

        return AnalysisResultMapper.ToResult(input, model, inputTokens, outputTokens);
    }

    private async Task<MessagesResponse> SendAsync(MessagesRequest request, CancellationToken ct)
    {
        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.PostAsJsonAsync("messages", request, ApiJsonOptions, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is TimeoutRejectedException or TaskCanceledException)
        {
            logger.LogWarning("Claude request timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);
            throw new AiAnalysisException("The AI analysis took too long. Please try again.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException)
        {
            logger.LogWarning(ex, "Claude request failed");
            throw new AiAnalysisException(GenericFailureMessage, ex);
        }

        using (httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await ReadAndLogErrorAsync(httpResponse, ct);
                throw new AiAnalysisException(httpResponse.StatusCode switch
                {
                    _ when error?.Message?.Contains("credit balance is too low", StringComparison.OrdinalIgnoreCase) == true =>
                        "The AI service has no remaining credit.",
                    HttpStatusCode.TooManyRequests =>
                        "The AI service is receiving too many requests. Please wait a minute and try again.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "The AI service rejected the server's credentials. Please contact the site owner.",
                    _ => GenericFailureMessage
                });
            }

            try
            {
                return await httpResponse.Content.ReadFromJsonAsync<MessagesResponse>(ApiJsonOptions, ct)
                       ?? throw new JsonException("Empty response body.");
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Claude response body could not be parsed");
                throw new AiAnalysisException("The AI returned an unexpected response. Please try again.", ex);
            }
        }
    }

    // Logs the provider's error type and message only. Request content (resume, job description) is never logged.
    private async Task<ErrorDetail?> ReadAndLogErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ErrorDetail? error = null;
        try
        {
            error = (await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiJsonOptions, ct))?.Error;
        }
        catch (JsonException)
        {
            // Not a JSON error body; the status code is still logged below.
        }

        logger.LogWarning("Claude API returned {StatusCode}: {ErrorType} {ErrorMessage}",
            (int)response.StatusCode, error?.Type, error?.Message);
        return error;
    }

    private ReportAnalysisInput DeserializeToolInput(JsonElement input)
    {
        try
        {
            return AnalysisResultMapper.Parse(input);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Claude {ToolName} input was malformed: {Reason}", AnalysisPrompt.ToolName, ex.Message);
            throw new AiAnalysisException("The AI returned an incomplete analysis. Please try again.", ex);
        }
    }
}
