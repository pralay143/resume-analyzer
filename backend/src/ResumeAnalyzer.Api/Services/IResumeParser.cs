namespace ResumeAnalyzer.Api.Services;

public interface IResumeParser
{
    /// <summary>
    /// Validates the uploaded file and extracts its text.
    /// </summary>
    /// <exception cref="ResumeParseException">The file is invalid or its text can't be extracted.</exception>
    Task<ResumeParseResult> ParseAsync(IFormFile file, CancellationToken ct);
}
