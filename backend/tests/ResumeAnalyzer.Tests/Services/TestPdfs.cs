using System.Text;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace ResumeAnalyzer.Tests.Services;

/// <summary>
/// Builds PDFs in memory so no binary fixtures need to be committed.
/// </summary>
internal static class TestPdfs
{
    public static readonly string[] ResumeLines =
    [
        "Jane Doe - Senior Software Engineer",
        "Experienced in C#, ASP.NET Core, PostgreSQL and Angular.",
        "Built REST APIs serving millions of requests per day.",
        "Led a team of five engineers delivering cloud migrations."
    ];

    /// <summary>Creates a PDF with one page per entry; each page holds the given lines.</summary>
    public static byte[] Create(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var lines in pages)
        {
            var page = builder.AddPage(PageSize.A4);
            for (var i = 0; i < lines.Length; i++)
            {
                page.AddText(lines[i], 11, new PdfPoint(50, 780 - i * 16), font);
            }
        }

        return builder.Build();
    }

    /// <summary>Creates a one-page PDF with two side-by-side columns of text.</summary>
    public static byte[] CreateTwoColumn(string[] leftColumn, string[] rightColumn)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);

        for (var i = 0; i < leftColumn.Length; i++)
        {
            page.AddText(leftColumn[i], 10, new PdfPoint(40, 780 - i * 14), font);
        }

        for (var i = 0; i < rightColumn.Length; i++)
        {
            page.AddText(rightColumn[i], 10, new PdfPoint(320, 780 - i * 14), font);
        }

        return builder.Build();
    }

    public static IFormFile ToFormFile(byte[] content, string fileName = "resume.pdf") =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);

    public static IFormFile ToFormFile(string content, string fileName) =>
        ToFormFile(Encoding.UTF8.GetBytes(content), fileName);
}
