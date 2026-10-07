using ResumeAnalyzer.Api.Services;
using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Tests.Services;

public class MatchScoringServiceTests
{
    private readonly MatchScoringService _scoring = new();

    private static readonly JobSkill[] MixedJobSkills =
    [
        new("Angular", "required"),
        new("TypeScript", "required"),
        new("PostgreSQL", "required"),
        new("Docker", "preferred"),
        new("AWS", "preferred")
    ];

    [Fact]
    public void Calculate_AllSkillsMatched_Returns100()
    {
        var result = _scoring.Calculate(MixedJobSkills, ["Angular", "TypeScript", "PostgreSQL", "Docker", "AWS"]);

        Assert.Equal(new MatchScoreResult(100, true, 3, 3, 2, 2), result);
    }

    [Fact]
    public void Calculate_NoSkillsMatched_Returns0()
    {
        var result = _scoring.Calculate(MixedJobSkills, []);

        Assert.Equal(new MatchScoreResult(0, true, 0, 3, 0, 2), result);
    }

    [Fact]
    public void Calculate_MixedMatch_WeighsRequiredDouble()
    {
        // (2 required x 2 + 1 preferred x 1) / (3 x 2 + 2 x 1) = 5 / 8 = 62.5, rounded away from zero to 63.
        var result = _scoring.Calculate(MixedJobSkills, ["Angular", "TypeScript", "Docker"]);

        Assert.Equal(63, result.Score);
        Assert.Equal(2, result.MatchedRequired);
        Assert.Equal(1, result.MatchedPreferred);
    }

    [Fact]
    public void Calculate_OnlyRequiredSkills_ScoresFromRequiredOnly()
    {
        JobSkill[] jobSkills = [new("C#", "required"), new("ASP.NET Core", "required"), new("SQL", "required")];

        var result = _scoring.Calculate(jobSkills, ["C#"]);

        Assert.Equal(new MatchScoreResult(33, true, 1, 3, 0, 0), result);
    }

    [Fact]
    public void Calculate_OnlyPreferredSkills_ScoresFromPreferredOnly()
    {
        JobSkill[] jobSkills = [new("Kubernetes", "preferred"), new("Terraform", "preferred")];

        var result = _scoring.Calculate(jobSkills, ["Terraform"]);

        Assert.Equal(new MatchScoreResult(50, true, 0, 0, 1, 2), result);
    }

    [Fact]
    public void Calculate_NoJobSkills_Returns0AndFlagsIt()
    {
        var result = _scoring.Calculate([], ["Angular"]);

        Assert.Equal(0, result.Score);
        Assert.False(result.HasJobSkills);
    }

    [Fact]
    public void Calculate_CaseAndWhitespaceDifferences_StillMatch()
    {
        JobSkill[] jobSkills = [new("PostgreSQL", "REQUIRED"), new("aws", "Preferred")];

        var result = _scoring.Calculate(jobSkills, ["postgresql", " AWS "]);

        Assert.Equal(new MatchScoreResult(100, true, 1, 1, 1, 1), result);
    }

    [Fact]
    public void Calculate_MatchedSkillNotInJob_IsIgnored()
    {
        JobSkill[] jobSkills = [new("Angular", "required"), new("RxJS", "required")];

        var result = _scoring.Calculate(jobSkills, ["Angular", "React"]);

        Assert.Equal(50, result.Score);
    }

    [Fact]
    public void Calculate_DuplicateJobSkills_AreCountedOnce()
    {
        JobSkill[] jobSkills = [new("Angular", "required"), new("angular", "required"), new("Docker", "required")];

        var result = _scoring.Calculate(jobSkills, ["Angular"]);

        Assert.Equal(new MatchScoreResult(50, true, 1, 2, 0, 0), result);
    }
}
