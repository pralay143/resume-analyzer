using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Tests.Integration;

/// <summary>Stands in for Claude so integration tests never call the real API.</summary>
public sealed class FakeAiAnalysisService : IAiAnalysisService
{
    public static readonly AiAnalysisResult DefaultResult = new(
        Model: "claude-fake-model",
        InputTokens: 1500,
        OutputTokens: 400,
        ResumeSkills: [new ResumeSkill("Angular", "frontend"), new ResumeSkill("Docker", "devops")],
        JobSkills:
        [
            new JobSkill("Angular", "required"),
            new JobSkill("TypeScript", "required"),
            new JobSkill("Docker", "preferred")
        ],
        MatchedSkills: ["Angular", "Docker"],
        MissingRequiredSkills: ["TypeScript"],
        MissingPreferredSkills: [],
        Suggestions: ["If you have TypeScript experience, add it to your skills section.", "Quantify results.", "Tailor the summary."],
        Summary: "Good frontend match. TypeScript is not shown.");

    public AiAnalysisResult Result { get; set; } = DefaultResult;

    /// <summary>When set, AnalyzeAsync throws this instead of returning <see cref="Result"/>.</summary>
    public Exception? Exception { get; set; }

    public int CallCount { get; private set; }

    public Task<AiAnalysisResult> AnalyzeAsync(string resumeText, string jobDescription, CancellationToken ct)
    {
        CallCount++;
        return Exception is null ? Task.FromResult(Result) : Task.FromException<AiAnalysisResult>(Exception);
    }

    public void Reset()
    {
        Result = DefaultResult;
        Exception = null;
        CallCount = 0;
    }
}
