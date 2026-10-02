using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ResumeAnalyzer.Api.Models.Dtos;

public class AiTestRequest
{
    /// <summary>The resume as a text-based PDF, at most 5 MB.</summary>
    [Required]
    [FromForm(Name = "resume")]
    public IFormFile Resume { get; set; } = null!;

    /// <summary>The job description as plain text.</summary>
    [Required]
    [FromForm(Name = "jobDescription")]
    [MaxLength(20_000)]
    public string JobDescription { get; set; } = string.Empty;
}
