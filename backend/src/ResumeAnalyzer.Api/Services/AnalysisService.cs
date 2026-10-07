using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Api.Data;
using ResumeAnalyzer.Api.Models.Dtos;
using ResumeAnalyzer.Api.Models.Entities;
using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Api.Services;

public class AnalysisService(
    AppDbContext db,
    IResumeParser resumeParser,
    IAiAnalysisService aiAnalysisService,
    IMatchScoringService matchScoringService,
    ILogger<AnalysisService> logger) : IAnalysisService
{
    private const int ScoreTrendLength = 30;
    private const int TopMissingSkillsCount = 10;

    public async Task<AnalysisDto> CreateAsync(CreateAnalysisRequest request, CancellationToken ct)
    {
        var resume = await resumeParser.ParseAsync(request.Resume, ct);
        var ai = await aiAnalysisService.AnalyzeAsync(resume.Text, request.JobDescription, ct);
        var score = matchScoringService.Calculate(ai.JobSkills, ai.MatchedSkills);

        if (!score.HasJobSkills)
        {
            logger.LogWarning("The AI found no skills in the job description; saving the analysis with a score of 0");
        }

        var analysis = new Analysis
        {
            JobTitle = request.JobTitle.Trim(),
            CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
            ResumeFileName = resume.FileName,
            ResumeText = resume.Text,
            JobDescription = request.JobDescription,
            MatchScore = score.Score,
            MatchedSkills = [.. ai.MatchedSkills],
            MissingRequiredSkills = [.. ai.MissingRequiredSkills],
            MissingPreferredSkills = [.. ai.MissingPreferredSkills],
            Suggestions = [.. ai.Suggestions],
            Summary = ai.Summary,
            AiModel = ai.Model,
            InputTokens = ai.InputTokens,
            OutputTokens = ai.OutputTokens
        };

        db.Analyses.Add(analysis);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Saved analysis {AnalysisId} with score {MatchScore}", analysis.Id, analysis.MatchScore);

        return ToDto(analysis);
    }

    public async Task<PagedResult<AnalysisListItemDto>> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await db.Analyses.CountAsync(ct);

        var items = await db.Analyses
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnalysisListItemDto(a.Id, a.JobTitle, a.CompanyName, a.MatchScore, a.CreatedAt))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<AnalysisListItemDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<AnalysisDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var analysis = await db.Analyses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        return analysis is null ? null : ToDto(analysis);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) =>
        await db.Analyses.Where(a => a.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<AnalysisStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var totalAnalyses = await db.Analyses.CountAsync(ct);
        var averageScore = await db.Analyses.AverageAsync(a => (double?)a.MatchScore, ct) ?? 0;

        var recent = await db.Analyses
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(ScoreTrendLength)
            .Select(a => new ScoreTrendPointDto(a.CreatedAt, a.MatchScore))
            .ToListAsync(ct);
        recent.Reverse(); // Oldest first, ready for a chart.

        // Counts each missing skill across both text[] columns, case-insensitively, reporting its most common spelling.
        var topMissingSkills = await db.Database.SqlQuery<SkillCountDto>($"""
            SELECT mode() WITHIN GROUP (ORDER BY skill) AS "Skill", count(*)::int AS "Count"
            FROM (
                SELECT unnest(missing_required_skills) AS skill FROM analyses
                UNION ALL
                SELECT unnest(missing_preferred_skills) AS skill FROM analyses
            ) AS missing
            GROUP BY lower(skill)
            ORDER BY count(*) DESC, lower(skill)
            LIMIT {TopMissingSkillsCount}
            """).ToListAsync(ct);

        return new AnalysisStatsDto(totalAnalyses, Math.Round(averageScore, 1), recent, topMissingSkills);
    }

    private static AnalysisDto ToDto(Analysis a) => new(
        a.Id,
        a.JobTitle,
        a.CompanyName,
        a.ResumeFileName,
        a.JobDescription,
        a.MatchScore,
        a.MatchedSkills,
        a.MissingRequiredSkills,
        a.MissingPreferredSkills,
        a.Suggestions,
        a.Summary,
        a.AiModel,
        a.InputTokens,
        a.OutputTokens,
        a.CreatedAt);
}
