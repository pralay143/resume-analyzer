using Microsoft.AspNetCore.Mvc;
using ResumeAnalyzer.Api.Services;

namespace ResumeAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController(IResumeParser resumeParser, IWebHostEnvironment environment) : ControllerBase
{
    // Leaves headroom over the 5 MB file limit for multipart overhead, so slightly-too-large files
    // reach the parser and get its clearer error message.
    private const long MaxRequestBytes = 6 * 1024 * 1024;

    /// <summary>
    /// Temporary, development-only: extracts text from an uploaded PDF resume.
    /// </summary>
    [HttpPost("parse")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<ResumeParseResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Parse(IFormFile file, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await resumeParser.ParseAsync(file, cancellationToken);
        return Ok(result);
    }
}
