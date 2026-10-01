using Microsoft.AspNetCore.Mvc;
using ResumeAnalyzer.Api.Data;

namespace ResumeAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            timestamp = DateTimeOffset.UtcNow,
            database = new { canConnect }
        });
    }
}
