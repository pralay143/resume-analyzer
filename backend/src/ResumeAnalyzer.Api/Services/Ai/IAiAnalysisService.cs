namespace ResumeAnalyzer.Api.Services.Ai;

public interface IAiAnalysisService
{
    /// <summary>
    /// Extracts and compares the skills in a resume and a job description, and suggests improvements.
    /// </summary>
    /// <exception cref="AiAnalysisException">The AI provider failed or returned an unusable response.</exception>
    Task<AiAnalysisResult> AnalyzeAsync(string resumeText, string jobDescription, CancellationToken ct);
}
