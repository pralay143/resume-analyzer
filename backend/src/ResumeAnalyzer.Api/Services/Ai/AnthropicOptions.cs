using System.ComponentModel.DataAnnotations;

namespace ResumeAnalyzer.Api.Services.Ai;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    [Required(ErrorMessage =
        "Anthropic:ApiKey is missing. For local development run " +
        "'dotnet user-secrets set \"Anthropic:ApiKey\" \"<your-key>\"' in backend/src/ResumeAnalyzer.Api, " +
        "or set the Anthropic__ApiKey environment variable.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Anthropic:Model is missing.")]
    public string Model { get; set; } = string.Empty;

    [Range(256, 64_000)]
    public int MaxTokens { get; set; } = 2000;

    [Range(10, 600)]
    public int TimeoutSeconds { get; set; } = 90;
}
