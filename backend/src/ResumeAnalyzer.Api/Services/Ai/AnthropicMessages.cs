using System.Text.Json;

namespace ResumeAnalyzer.Api.Services.Ai;

// Wire types for the Anthropic Messages API (https://docs.anthropic.com/en/api/messages).
// Serialized with snake_case naming, so e.g. MaxTokens is sent as "max_tokens".

internal sealed record MessagesRequest(
    string Model,
    int MaxTokens,
    double Temperature,
    string System,
    IReadOnlyList<ToolDefinition> Tools,
    ToolChoice ToolChoice,
    IReadOnlyList<RequestMessage> Messages);

internal sealed record ToolDefinition(string Name, string Description, JsonElement InputSchema);

internal sealed record ToolChoice(string Type, string Name);

internal sealed record RequestMessage(string Role, string Content);

internal sealed record MessagesResponse(
    string? Model,
    IReadOnlyList<ContentBlock>? Content,
    string? StopReason,
    TokenUsage? Usage);

internal sealed record ContentBlock(string? Type, string? Name, JsonElement Input);

internal sealed record TokenUsage(int InputTokens, int OutputTokens);

internal sealed record ErrorResponse(ErrorDetail? Error);

internal sealed record ErrorDetail(string? Type, string? Message);

// Shape of the report_analysis tool input. Property names are camelCase, matching the tool schema.
internal sealed record ReportAnalysisInput(
    IReadOnlyList<ResumeSkillInput?>? ResumeSkills,
    IReadOnlyList<JobSkillInput?>? JobSkills,
    IReadOnlyList<string?>? MatchedSkills,
    IReadOnlyList<string?>? MissingRequiredSkills,
    IReadOnlyList<string?>? MissingPreferredSkills,
    IReadOnlyList<string?>? Suggestions,
    string? Summary);

internal sealed record ResumeSkillInput(string? Name, string? Category);

internal sealed record JobSkillInput(string? Name, string? Importance);
