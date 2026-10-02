using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ResumeAnalyzer.Api.Models.Dtos;
using ResumeAnalyzer.Api.RateLimiting;
using ResumeAnalyzer.Api.Services;

namespace ResumeAnalyzer.Api.Controllers;

[ApiController]
[Route("api/analyses")]
public class AnalysesController(IAnalysisService analysisService) : ControllerBase
{
    // Leaves headroom over the 5 MB file limit for multipart overhead and the text fields.
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    /// <summary>Analyzes a PDF resume against a job description and saves the result.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.AnalysesPolicy)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<AnalysisDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromForm] CreateAnalysisRequest request, CancellationToken ct)
    {
        var analysis = await analysisService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = analysis.Id }, analysis);
    }

    /// <summary>Lists analyses, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AnalysisListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<PagedResult<AnalysisListItemDto>> GetPage(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 50)] int pageSize = 10,
        CancellationToken ct = default) =>
        await analysisService.GetPageAsync(page, pageSize, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AnalysisDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var analysis = await analysisService.GetByIdAsync(id, ct);
        return analysis is null ? NotFound() : Ok(analysis);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await analysisService.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Totals, score trend and most common missing skills across all analyses.</summary>
    [HttpGet("stats")]
    [ProducesResponseType<AnalysisStatsDto>(StatusCodes.Status200OK)]
    public async Task<AnalysisStatsDto> GetStats(CancellationToken ct) =>
        await analysisService.GetStatsAsync(ct);
}
