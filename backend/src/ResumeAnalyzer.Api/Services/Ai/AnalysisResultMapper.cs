using System.Text.Json;

namespace ResumeAnalyzer.Api.Services.Ai;

/// <summary>
/// Validates and cleans the structured analysis returned by any AI provider, so every provider
/// produces results with the same guarantees.
/// </summary>
internal static class AnalysisResultMapper
{
    private static readonly JsonSerializerOptions InputJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the analysis object, matching the fields of <see cref="AnalysisPrompt.ToolInputSchema"/>.</summary>
    /// <exception cref="JsonException">The value isn't a usable analysis; the message says why.</exception>
    public static ReportAnalysisInput Parse(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Analysis is {input.ValueKind}, expected an object.");
        }

        var parsed = input.Deserialize<ReportAnalysisInput>(InputJsonOptions)
                     ?? throw new JsonException("Analysis is null.");

        if (string.IsNullOrWhiteSpace(parsed.Summary))
        {
            throw new JsonException("Analysis has no summary.");
        }

        return parsed;
    }

    public static AiAnalysisResult ToResult(ReportAnalysisInput input, string model, int inputTokens, int outputTokens)
    {
        var resumeSkills = DistinctByName(input.ResumeSkills, s => s?.Name)
            .Select(s => new ResumeSkill(s.Name!.Trim(), Normalize(s.Category, AnalysisPrompt.SkillCategories, "other")))
            .ToList();

        var jobSkills = DistinctByName(input.JobSkills, s => s?.Name)
            .Select(s => new JobSkill(s.Name!.Trim(), Normalize(s.Importance, AnalysisPrompt.SkillImportances, "required")))
            .ToList();

        return new AiAnalysisResult(
            model,
            inputTokens,
            outputTokens,
            resumeSkills,
            jobSkills,
            CleanList(input.MatchedSkills),
            CleanList(input.MissingRequiredSkills),
            CleanList(input.MissingPreferredSkills),
            CleanList(input.Suggestions),
            input.Summary!.Trim());
    }

    // Drops null entries and blank names, and keeps the first of any case-insensitive duplicates.
    private static IEnumerable<T> DistinctByName<T>(IEnumerable<T?>? items, Func<T?, string?> name) where T : class =>
        (items ?? [])
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(name(item)))
            .DistinctBy(item => name(item)!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(item => item!);

    private static List<string> CleanList(IEnumerable<string?>? items) =>
        DistinctByName(items, s => s).Select(s => s.Trim()).ToList();

    private static string Normalize(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is not null && allowed.Contains(normalized) ? normalized : fallback;
    }
}
