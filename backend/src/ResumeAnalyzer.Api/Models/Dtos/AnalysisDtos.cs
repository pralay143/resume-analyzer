namespace ResumeAnalyzer.Api.Models.Dtos;

/// <summary>A full analysis. The resume text is deliberately left out.</summary>
public record AnalysisDto(
    Guid Id,
    string JobTitle,
    string? CompanyName,
    string ResumeFileName,
    string JobDescription,
    int MatchScore,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingRequiredSkills,
    IReadOnlyList<string> MissingPreferredSkills,
    IReadOnlyList<string> Suggestions,
    string Summary,
    string AiModel,
    int InputTokens,
    int OutputTokens,
    DateTime CreatedAt);

public record AnalysisListItemDto(
    Guid Id,
    string JobTitle,
    string? CompanyName,
    int MatchScore,
    DateTime CreatedAt);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record AnalysisStatsDto(
    int TotalAnalyses,
    double AverageScore,
    IReadOnlyList<ScoreTrendPointDto> ScoreTrend,
    IReadOnlyList<SkillCountDto> TopMissingSkills);

public record ScoreTrendPointDto(DateTime Date, int Score);

// A class with settable properties (not a positional record) because EF Core's SqlQuery materializes it.
public class SkillCountDto
{
    public string Skill { get; init; } = string.Empty;
    public int Count { get; init; }
}
