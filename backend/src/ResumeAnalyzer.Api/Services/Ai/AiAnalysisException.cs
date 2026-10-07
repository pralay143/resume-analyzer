namespace ResumeAnalyzer.Api.Services.Ai;

/// <summary>
/// Thrown when the AI provider fails or returns an unusable response. The message is safe to show to the user.
/// </summary>
public class AiAnalysisException(string message, Exception? innerException = null)
    : Exception(message, innerException);
