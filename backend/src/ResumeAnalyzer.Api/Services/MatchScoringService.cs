using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Api.Services;

public class MatchScoringService : IMatchScoringService
{
    private const int RequiredWeight = 2;
    private const int PreferredWeight = 1;

    public MatchScoreResult Calculate(IReadOnlyCollection<JobSkill> jobSkills, IReadOnlyCollection<string> matchedSkills)
    {
        var matched = new HashSet<string>(matchedSkills.Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);

        var distinctJobSkills = jobSkills
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .DistinctBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var required = distinctJobSkills.Where(s => IsRequired(s.Importance)).ToList();
        var preferred = distinctJobSkills.Where(s => !IsRequired(s.Importance)).ToList();

        var matchedRequired = required.Count(s => matched.Contains(s.Name.Trim()));
        var matchedPreferred = preferred.Count(s => matched.Contains(s.Name.Trim()));

        var possible = required.Count * RequiredWeight + preferred.Count * PreferredWeight;
        if (possible == 0)
        {
            return new MatchScoreResult(0, HasJobSkills: false, 0, 0, 0, 0);
        }

        var earned = matchedRequired * RequiredWeight + matchedPreferred * PreferredWeight;
        var score = (int)Math.Round(earned * 100.0 / possible, MidpointRounding.AwayFromZero);

        return new MatchScoreResult(score, HasJobSkills: true, matchedRequired, required.Count, matchedPreferred, preferred.Count);
    }

    private static bool IsRequired(string importance) =>
        !string.Equals(importance, "preferred", StringComparison.OrdinalIgnoreCase);
}
