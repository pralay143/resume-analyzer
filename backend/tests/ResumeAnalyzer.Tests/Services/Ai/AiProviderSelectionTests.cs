using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Tests.Services.Ai;

public class AiProviderSelectionTests
{
    [Fact]
    public void AddAiAnalysis_NoProviderConfigured_DefaultsToGemini()
    {
        using var provider = BuildProvider(new() { ["Gemini:ApiKey"] = "gemini-key", ["Gemini:Model"] = "gemini-test" });

        Assert.IsType<GeminiAnalysisService>(provider.GetRequiredService<IAiAnalysisService>());
    }

    [Fact]
    public void AddAiAnalysis_Gemini_DoesNotRequireClaudeKey()
    {
        using var provider = BuildProvider(new()
        {
            ["Ai:Provider"] = "Gemini",
            ["Gemini:ApiKey"] = "gemini-key",
            ["Gemini:Model"] = "gemini-test"
        });

        // Startup validation only covers the selected provider's options.
        RunStartupValidation(provider);
        Assert.IsType<GeminiAnalysisService>(provider.GetRequiredService<IAiAnalysisService>());
    }

    [Fact]
    public void AddAiAnalysis_Claude_DoesNotRequireGeminiKey()
    {
        using var provider = BuildProvider(new()
        {
            ["Ai:Provider"] = "Claude",
            ["Anthropic:ApiKey"] = "claude-key",
            ["Anthropic:Model"] = "claude-test"
        });

        RunStartupValidation(provider);
        Assert.IsType<ClaudeAnalysisService>(provider.GetRequiredService<IAiAnalysisService>());
    }

    [Fact]
    public void AddAiAnalysis_SelectedProviderKeyMissing_FailsStartupValidation()
    {
        using var provider = BuildProvider(new() { ["Ai:Provider"] = "Gemini", ["Gemini:Model"] = "gemini-test" });

        // Thrown as an OptionsValidationException, possibly wrapped in an AggregateException.
        var ex = Assert.ThrowsAny<Exception>(() => RunStartupValidation(provider));
        Assert.Contains("Gemini:ApiKey is missing", ex.ToString());
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAiAnalysis(configuration);
        return services.BuildServiceProvider();
    }

    // The same check the host runs at startup for options registered with ValidateOnStart().
    private static void RunStartupValidation(IServiceProvider provider) =>
        provider.GetRequiredService<IStartupValidator>().Validate();
}
