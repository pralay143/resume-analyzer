namespace ResumeAnalyzer.Api.Services;

/// <summary>
/// Thrown when an uploaded resume can't be accepted or read. The message is safe to show to the user.
/// </summary>
public class ResumeParseException(string message, Exception? innerException = null)
    : Exception(message, innerException);
