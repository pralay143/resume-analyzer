namespace ResumeAnalyzer.Api.Services.Ai;

public enum AiProvider
{
    Gemini,
    Claude
}

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Which provider implements <see cref="IAiAnalysisService"/>. Only its API key is required.</summary>
    public AiProvider Provider { get; set; } = AiProvider.Gemini;
}
