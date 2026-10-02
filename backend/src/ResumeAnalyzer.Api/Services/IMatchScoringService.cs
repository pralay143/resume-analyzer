using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Api.Services;

public interface IMatchScoringService
{
    /// <summary>
    /// Scores how well the matched skills cover the job's skills. Required skills weigh twice as much as preferred.
    /// </summary>
    MatchScoreResult Calculate(IReadOnlyCollection<JobSkill> jobSkills, IReadOnlyCollection<string> matchedSkills);
}

/// <param name="Score">0–100. Always 0 when <paramref name="HasJobSkills"/> is false.</param>
/// <param name="HasJobSkills">False when the job description listed no skills, so the score is meaningless.</param>
public record MatchScoreResult(
    int Score,
    bool HasJobSkills,
    int MatchedRequired,
    int TotalRequired,
    int MatchedPreferred,
    int TotalPreferred);
