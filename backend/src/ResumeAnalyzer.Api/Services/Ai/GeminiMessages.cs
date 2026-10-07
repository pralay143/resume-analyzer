using System.Text.Json;

namespace ResumeAnalyzer.Api.Services.Ai;

// Wire types for the Gemini generateContent API (https://ai.google.dev/api/generate-content).
// Serialized with camelCase naming; null properties are omitted.

internal sealed record GenerateContentRequest(
    GeminiContent SystemInstruction,
    IReadOnlyList<GeminiContent> Contents,
    GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiContent(IReadOnlyList<GeminiPart> Parts, string? Role = null);

internal sealed record GeminiPart(string? Text, bool? Thought = null);

internal sealed record GeminiGenerationConfig(
    double Temperature,
    int MaxOutputTokens,
    string ResponseMimeType,
    JsonElement ResponseJsonSchema,
    GeminiThinkingConfig? ThinkingConfig);

internal sealed record GeminiThinkingConfig(string ThinkingLevel);

internal sealed record GenerateContentResponse(
    IReadOnlyList<GeminiCandidate>? Candidates,
    GeminiUsageMetadata? UsageMetadata,
    string? ModelVersion,
    GeminiPromptFeedback? PromptFeedback);

internal sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

internal sealed record GeminiUsageMetadata(int PromptTokenCount, int CandidatesTokenCount, int ThoughtsTokenCount);

internal sealed record GeminiPromptFeedback(string? BlockReason);

internal sealed record GeminiErrorResponse(GeminiError? Error);

internal sealed record GeminiError(int Code, string? Message, string? Status);
