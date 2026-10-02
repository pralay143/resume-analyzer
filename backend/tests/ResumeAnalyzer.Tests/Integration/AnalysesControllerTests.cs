using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ResumeAnalyzer.Api.Models.Dtos;
using ResumeAnalyzer.Api.Models.Entities;
using ResumeAnalyzer.Api.Services.Ai;
using ResumeAnalyzer.Tests.Services;

namespace ResumeAnalyzer.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AnalysesControllerTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_ValidRequest_Returns201WithLocationAndSavesScoredAnalysis()
    {
        using var response = await _client.PostAsync("/api/analyses", CreateForm(companyName: "Contoso"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AnalysisDto>();
        Assert.NotNull(created);
        Assert.Equal($"/api/analyses/{created.Id}", response.Headers.Location?.AbsolutePath);

        // Fake AI: Angular and TypeScript required, Docker preferred; Angular and Docker matched.
        // (1 x 2 + 1 x 1) / (2 x 2 + 1 x 1) = 3 / 5 = 60.
        Assert.Equal(60, created.MatchScore);
        Assert.Equal("Senior Angular Developer", created.JobTitle);
        Assert.Equal("Contoso", created.CompanyName);
        Assert.Equal("cv.pdf", created.ResumeFileName);
        Assert.Equal(["Angular", "Docker"], created.MatchedSkills);
        Assert.Equal(["TypeScript"], created.MissingRequiredSkills);
        Assert.Equal("claude-fake-model", created.AiModel);
        Assert.Equal(1500, created.InputTokens);
        Assert.True(created.CreatedAt > DateTime.UtcNow.AddMinutes(-5));

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("resumeText", json, StringComparison.OrdinalIgnoreCase);

        var saved = await factory.WithDbAsync(db => db.Analyses.SingleAsync());
        Assert.Equal(created.Id, saved.Id);
        Assert.Contains("Jane Doe", saved.ResumeText);
    }

    [Fact]
    public async Task Post_BlankCompanyName_IsSavedAsNull()
    {
        using var response = await _client.PostAsync("/api/analyses", CreateForm(companyName: "   "));

        var created = await response.Content.ReadFromJsonAsync<AnalysisDto>();
        Assert.Null(created!.CompanyName);
    }

    [Fact]
    public async Task Post_MissingJobTitle_Returns400WithoutCallingAi()
    {
        using var response = await _client.PostAsync("/api/analyses", CreateForm(jobTitle: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("jobTitle", problem!.Errors.Keys);
        Assert.Equal(0, factory.FakeAi.CallCount);
    }

    [Fact]
    public async Task Post_JobTitleTooLong_Returns400()
    {
        using var response = await _client.PostAsync("/api/analyses", CreateForm(jobTitle: new string('x', 201)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidPdf_Returns400ProblemDetailsWithoutCallingAi()
    {
        using var response = await _client.PostAsync("/api/analyses",
            CreateForm(resume: "not a pdf"u8.ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid resume file", problem!.Title);
        Assert.Equal(0, factory.FakeAi.CallCount);
    }

    [Fact]
    public async Task Post_AiFailure_Returns502AndSavesNothing()
    {
        factory.FakeAi.Exception = new AiAnalysisException("The AI analysis service is unavailable right now.");

        using var response = await _client.PostAsync("/api/analyses", CreateForm());

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("The AI analysis service is unavailable right now.", problem!.Detail);
        Assert.Equal(0, await factory.WithDbAsync(db => db.Analyses.CountAsync()));
    }

    [Fact]
    public async Task Post_JobWithNoSkills_SavesScoreOfZero()
    {
        factory.FakeAi.Result = FakeAiAnalysisService.DefaultResult with
        {
            JobSkills = [], MatchedSkills = [], MissingRequiredSkills = []
        };

        using var response = await _client.PostAsync("/api/analyses", CreateForm());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<AnalysisDto>())!.MatchScore);
    }

    [Fact]
    public async Task GetPage_ReturnsNewestFirstWithOnlySummaryFields()
    {
        await SeedAsync(
            NewAnalysis("Oldest", 40, daysAgo: 3),
            NewAnalysis("Newest", 90, daysAgo: 0),
            NewAnalysis("Middle", 70, daysAgo: 1));

        using var response = await _client.GetAsync("/api/analyses?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AnalysisListItemDto>>();
        Assert.Equal(["Newest", "Middle"], page!.Items.Select(i => i.JobTitle));
        Assert.Equal((1, 2, 3, 2), (page.Page, page.PageSize, page.TotalCount, page.TotalPages));

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var itemProperties = json.RootElement.GetProperty("items")[0].EnumerateObject().Select(p => p.Name);
        Assert.Equal(["id", "jobTitle", "companyName", "matchScore", "createdAt"], itemProperties);
    }

    [Fact]
    public async Task GetPage_SecondPage_ReturnsRemainingItems()
    {
        await SeedAsync(NewAnalysis("A", 10, daysAgo: 2), NewAnalysis("B", 20, daysAgo: 1), NewAnalysis("C", 30, daysAgo: 0));

        var page = await _client.GetFromJsonAsync<PagedResult<AnalysisListItemDto>>("/api/analyses?page=2&pageSize=2");

        Assert.Equal(["A"], page!.Items.Select(i => i.JobTitle));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=51")]
    public async Task GetPage_InvalidPaging_Returns400(string query)
    {
        using var response = await _client.GetAsync($"/api/analyses?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsFullAnalysisWithoutResumeText()
    {
        var analysis = NewAnalysis("Backend Engineer", 75, daysAgo: 0);
        await SeedAsync(analysis);

        using var response = await _client.GetAsync($"/api/analyses/{analysis.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AnalysisDto>();
        Assert.Equal("Backend Engineer", dto!.JobTitle);
        Assert.Equal(analysis.Suggestions, dto.Suggestions);
        Assert.DoesNotContain("resumeText", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        using var response = await _client.GetAsync($"/api/analyses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existing_Returns204AndRemovesIt()
    {
        var analysis = NewAnalysis("To delete", 50, daysAgo: 0);
        await SeedAsync(analysis);

        using var deleteResponse = await _client.DeleteAsync($"/api/analyses/{analysis.Id}");
        using var getResponse = await _client.GetAsync($"/api/analyses/{analysis.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        using var response = await _client.DeleteAsync($"/api/analyses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_ComputesTotalsTrendAndTopMissingSkills()
    {
        await SeedAsync(
            NewAnalysis("A", 40, daysAgo: 2, missingRequired: ["Docker", "AWS"], missingPreferred: ["Kubernetes"]),
            NewAnalysis("B", 60, daysAgo: 1, missingRequired: ["docker"], missingPreferred: ["AWS"]),
            NewAnalysis("C", 85, daysAgo: 0, missingRequired: ["Docker"]));

        var stats = await _client.GetFromJsonAsync<AnalysisStatsDto>("/api/analyses/stats");

        Assert.Equal(3, stats!.TotalAnalyses);
        Assert.Equal(61.7, stats.AverageScore);
        Assert.Equal([40, 60, 85], stats.ScoreTrend.Select(p => p.Score));
        Assert.Equal(
            [("Docker", 3), ("AWS", 2), ("Kubernetes", 1)],
            stats.TopMissingSkills.Select(s => (s.Skill, s.Count)));
    }

    [Fact]
    public async Task GetStats_NoAnalyses_ReturnsZeros()
    {
        var stats = await _client.GetFromJsonAsync<AnalysisStatsDto>("/api/analyses/stats");

        Assert.Equal(0, stats!.TotalAnalyses);
        Assert.Equal(0, stats.AverageScore);
        Assert.Empty(stats.ScoreTrend);
        Assert.Empty(stats.TopMissingSkills);
    }

    [Fact]
    public async Task GetStats_ScoreTrend_KeepsOnlyTheLast30()
    {
        await SeedAsync(Enumerable.Range(0, 35).Select(i => NewAnalysis($"Job {i}", i, daysAgo: 35 - i)).ToArray());

        var stats = await _client.GetFromJsonAsync<AnalysisStatsDto>("/api/analyses/stats");

        Assert.Equal(30, stats!.ScoreTrend.Count);
        Assert.Equal(5, stats.ScoreTrend[0].Score);
        Assert.Equal(34, stats.ScoreTrend[^1].Score);
    }

    [Fact]
    public async Task Post_OverRateLimit_Returns429WithRetryAfterPerClientIp()
    {
        using var limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Analyses:PermitLimit", "2"));
        var client = limitedFactory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await PostFromIpAsync(client, "203.0.113.10")).StatusCode);
        }

        using var rejected = await PostFromIpAsync(client, "203.0.113.10");
        using var otherClient = await PostFromIpAsync(client, "203.0.113.20");
        using var listResponse = await client.GetAsync("/api/analyses");

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.TooManyRequests], statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(int.Parse(rejected.Headers.GetValues("Retry-After").Single()) > 0);
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(429, problem!.Status);
        Assert.Contains("limit", problem.Detail);

        Assert.Equal(HttpStatusCode.Created, otherClient.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> PostFromIpAsync(HttpClient client, string ip)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/analyses") { Content = CreateForm() };
        request.Headers.Add("X-Forwarded-For", ip);
        return await client.SendAsync(request);
    }

    private static MultipartFormDataContent CreateForm(
        string? jobTitle = "Senior Angular Developer",
        string? companyName = null,
        string jobDescription = "Required: Angular, TypeScript. Nice to have: Docker.",
        byte[]? resume = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(resume ?? TestPdfs.Create(TestPdfs.ResumeLines));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "resume", "cv.pdf");
        form.Add(new StringContent(jobDescription), "jobDescription");
        if (jobTitle is not null)
        {
            form.Add(new StringContent(jobTitle), "jobTitle");
        }

        if (companyName is not null)
        {
            form.Add(new StringContent(companyName), "companyName");
        }

        return form;
    }

    private static Analysis NewAnalysis(
        string jobTitle,
        int score,
        int daysAgo,
        List<string>? missingRequired = null,
        List<string>? missingPreferred = null) => new()
    {
        Id = Guid.NewGuid(),
        JobTitle = jobTitle,
        ResumeFileName = "cv.pdf",
        ResumeText = "Resume text that must never be returned.",
        JobDescription = "Job description",
        MatchScore = score,
        MatchedSkills = ["Angular"],
        MissingRequiredSkills = missingRequired ?? [],
        MissingPreferredSkills = missingPreferred ?? [],
        Suggestions = ["Add metrics."],
        Summary = "Summary.",
        AiModel = "claude-fake-model",
        CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
    };

    private Task SeedAsync(params Analysis[] analyses) => factory.WithDbAsync(async db =>
    {
        db.Analyses.AddRange(analyses);
        return await db.SaveChangesAsync();
    });
}
