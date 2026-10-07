using ResumeAnalyzer.Api.Models.Dtos;

namespace ResumeAnalyzer.Api.Services;

public interface IAnalysisService
{
    /// <summary>Parses the resume, runs the AI analysis, scores it and saves the result.</summary>
    /// <exception cref="ResumeParseException">The resume file is invalid.</exception>
    /// <exception cref="Ai.AiAnalysisException">The AI analysis failed.</exception>
    Task<AnalysisDto> CreateAsync(CreateAnalysisRequest request, CancellationToken ct);

    Task<PagedResult<AnalysisListItemDto>> GetPageAsync(int page, int pageSize, CancellationToken ct);

    Task<AnalysisDto?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <returns>False if no analysis has that id.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    Task<AnalysisStatsDto> GetStatsAsync(CancellationToken ct);
}
