using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ResumeAnalyzer.Api.Models.Dtos;

public class CreateAnalysisRequest
{
    /// <summary>The resume as a text-based PDF, at most 5 MB.</summary>
    [Required]
    [FromForm(Name = "resume")]
    public IFormFile Resume { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    [FromForm(Name = "jobTitle")]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(200)]
    [FromForm(Name = "companyName")]
    public string? CompanyName { get; set; }

    /// <summary>The job description as plain text.</summary>
    [Required]
    [MaxLength(20_000)]
    [FromForm(Name = "jobDescription")]
    public string JobDescription { get; set; } = string.Empty;
}
