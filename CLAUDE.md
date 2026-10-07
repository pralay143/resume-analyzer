## Git workflow
- Never commit directly to main; work on the current feature branch
- Never commit secrets, appsettings.Development.json, or .env files
- Don't run git push; I'll review and push myself

## AI provider
- The AI provider is selectable with `Ai:Provider`: `Gemini` (default) or `Claude`.
- Only the selected provider's API key is required at startup: `Gemini:ApiKey` or `Anthropic:ApiKey`.
  Locally, store keys with `dotnet user-secrets` in `backend/src/ResumeAnalyzer.Api`, never in appsettings files.
- Both providers implement `IAiAnalysisService` and share the prompt rules and JSON schema (`AnalysisPrompt.cs`)
  and the result validation (`AnalysisResultMapper.cs`). Change those once, not per provider.
- Tests never call a real AI API; they use a fake `HttpMessageHandler` or `FakeAiAnalysisService`.
