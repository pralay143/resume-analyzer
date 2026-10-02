using Microsoft.AspNetCore.Http;
using ResumeAnalyzer.Api.Services;

namespace ResumeAnalyzer.Tests.Services;

public class PdfResumeParserTests
{
    private readonly PdfResumeParser _parser = new();

    [Fact]
    public async Task ParseAsync_ValidPdf_ReturnsCleanedTextAndMetadata()
    {
        var file = TestPdfs.ToFormFile(TestPdfs.Create(TestPdfs.ResumeLines));

        var result = await _parser.ParseAsync(file, CancellationToken.None);

        Assert.Equal("resume.pdf", result.FileName);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(result.Text.Length, result.CharacterCount);
        Assert.Contains("Jane Doe - Senior Software Engineer", result.Text);
        Assert.Contains("C#, ASP.NET Core, PostgreSQL and Angular.", result.Text);
        Assert.Equal(result.Text.Trim(), result.Text);
        Assert.DoesNotContain("  ", result.Text);
        Assert.DoesNotContain("\n\n\n", result.Text);
    }

    [Fact]
    public async Task ParseAsync_TwoColumnLayout_ReadsLeftColumnBeforeRightColumn()
    {
        string[] left = Enumerable.Range(1, 8).Select(i => $"Experience entry number {i} at Contoso").ToArray();
        string[] right = Enumerable.Range(1, 8).Select(i => $"Skill group {i}: Docker Kubernetes").ToArray();
        var file = TestPdfs.ToFormFile(TestPdfs.CreateTwoColumn(left, right));

        var result = await _parser.ParseAsync(file, CancellationToken.None);

        var lastLeft = result.Text.IndexOf(left[^1], StringComparison.Ordinal);
        var firstRight = result.Text.IndexOf(right[0], StringComparison.Ordinal);
        Assert.True(lastLeft >= 0 && firstRight >= 0, $"Both columns should be extracted. Text:\n{result.Text}");
        Assert.True(lastLeft < firstRight, $"Left column should be read before the right column. Text:\n{result.Text}");
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_Throws()
    {
        var file = TestPdfs.ToFormFile([], "resume.pdf");

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_NonPdfRenamedToPdf_Throws()
    {
        var file = TestPdfs.ToFormFile("This is a plain text file pretending to be a PDF resume.", "resume.pdf");

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("not a valid PDF", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_WrongExtension_Throws()
    {
        var file = TestPdfs.ToFormFile(TestPdfs.Create(TestPdfs.ResumeLines), "resume.docx");

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("Only PDF files", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_FileOverFiveMegabytes_Throws()
    {
        var content = new byte[PdfResumeParser.MaxFileSizeBytes + 1];
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", "resume.pdf");

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("5 MB", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_MoreThanFivePages_Throws()
    {
        var pages = Enumerable.Repeat(TestPdfs.ResumeLines, 6).ToArray();
        var file = TestPdfs.ToFormFile(TestPdfs.Create(pages));

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("6 pages", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_FivePages_IsAccepted()
    {
        var pages = Enumerable.Repeat(TestPdfs.ResumeLines, 5).ToArray();
        var file = TestPdfs.ToFormFile(TestPdfs.Create(pages));

        var result = await _parser.ParseAsync(file, CancellationToken.None);

        Assert.Equal(5, result.PageCount);
    }

    [Fact]
    public async Task ParseAsync_TextTooShort_ThrowsScannedPdfError()
    {
        var file = TestPdfs.ToFormFile(TestPdfs.Create(["Jane Doe"]));

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("scanned or image-only", ex.Message);
    }

    [Fact]
    public async Task ParseAsync_CorruptPdf_ThrowsFriendlyError()
    {
        var file = TestPdfs.ToFormFile("%PDF-1.7\nthis is not really a pdf body", "resume.pdf");

        var ex = await Assert.ThrowsAsync<ResumeParseException>(() => _parser.ParseAsync(file, CancellationToken.None));

        Assert.Contains("damaged", ex.Message);
    }

    [Fact]
    public void CleanText_CollapsesWhitespaceAndRemovesNonPrintableCharacters()
    {
        var cleaned = PdfResumeParser.CleanText("  Hello​   world \t\r\n\n\n\n Next\u0007 line  ");

        Assert.Equal("Hello world\n\nNext line", cleaned);
    }
}
