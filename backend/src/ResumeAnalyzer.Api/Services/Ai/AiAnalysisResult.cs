namespace ResumeAnalyzer.Api.Services.Ai;

public record ResumeSkill(string Name, string Category);

public record JobSkill(string Name, string Importance);

public record AiAnalysisResult(
    string Model,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<ResumeSkill> ResumeSkills,
    IReadOnlyList<JobSkill> JobSkills,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingRequiredSkills,
    IReadOnlyList<string> MissingPreferredSkills,
    IReadOnlyList<string> Suggestions,
    string Summary);
