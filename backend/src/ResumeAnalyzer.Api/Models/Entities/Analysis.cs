namespace ResumeAnalyzer.Api.Models.Entities;

public class Analysis
{
    public Guid Id { get; set; }

    // Populated once authentication is added.
    public string? UserId { get; set; }

    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    public string ResumeFileName { get; set; } = string.Empty;
    public string ResumeText { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;

    public int MatchScore { get; set; }

    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingRequiredSkills { get; set; } = [];
    public List<string> MissingPreferredSkills { get; set; } = [];
    public List<string> Suggestions { get; set; } = [];

    public string Summary { get; set; } = string.Empty;

    public string AiModel { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }

    public DateTime CreatedAt { get; set; }
}
