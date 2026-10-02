using System.ComponentModel.DataAnnotations;

namespace ResumeAnalyzer.Api.Services.Ai;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required(ErrorMessage =
        "Gemini:ApiKey is missing. Create a key at https://aistudio.google.com/apikey, then for local development run " +
        "'dotnet user-secrets set \"Gemini:ApiKey\" \"<your-key>\"' in backend/src/ResumeAnalyzer.Api, " +
        "or set the Gemini__ApiKey environment variable.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gemini:Model is missing.")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Models tried in order, once each, when the primary model is still overloaded (503) after its retries.
    /// </summary>
    public List<string> FallbackModels { get; set; } = [];

    /// <summary>Includes the model's thinking tokens, which Gemini counts against this limit.</summary>
    [Range(256, 65_536)]
    public int MaxOutputTokens { get; set; } = 8192;

    [Range(10, 600)]
    public int TimeoutSeconds { get; set; } = 90;

    /// <summary>
    /// Gemini 3 thinking level ("minimal", "low", "medium", "high"). Leave empty for models without thinking levels.
    /// </summary>
    public string? ThinkingLevel { get; set; } = "low";
}
