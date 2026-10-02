using Microsoft.AspNetCore.Mvc;
using ResumeAnalyzer.Api.Models.Dtos;
using ResumeAnalyzer.Api.Services;
using ResumeAnalyzer.Api.Services.Ai;

namespace ResumeAnalyzer.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(
    IResumeParser resumeParser,
    IAiAnalysisService aiAnalysisService,
    IWebHostEnvironment environment) : ControllerBase
{
    // Leaves headroom over the 5 MB file limit for multipart overhead and the job description.
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    /// <summary>
    /// Temporary, development-only: parses a PDF resume and runs the AI skill analysis against a job description.
    /// </summary>
    [HttpPost("test")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<AiAnalysisResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Test([FromForm] AiTestRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var resume = await resumeParser.ParseAsync(request.Resume, cancellationToken);
        var result = await aiAnalysisService.AnalyzeAsync(resume.Text, request.JobDescription, cancellationToken);
        return Ok(result);
    }
}
