namespace ResumeAnalyzer.Api.Services;

public record ResumeParseResult(string FileName, int PageCount, int CharacterCount, string Text);
