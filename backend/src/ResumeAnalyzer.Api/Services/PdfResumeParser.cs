using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Exceptions;

namespace ResumeAnalyzer.Api.Services;

public partial class PdfResumeParser : IResumeParser
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const int MaxPages = 5;
    public const int MinTextLength = 100;

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public async Task<ResumeParseResult> ParseAsync(IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            throw new ResumeParseException("The uploaded file is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ResumeParseException("The file is larger than 5 MB. Please upload a smaller PDF.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ResumeParseException("Only PDF files are supported. Please upload a .pdf file.");
        }

        var bytes = await ReadAllBytesAsync(file, ct);

        if (!bytes.AsSpan().StartsWith(PdfSignature))
        {
            throw new ResumeParseException("The file is not a valid PDF, even though its name ends in .pdf.");
        }

        var (pageCount, rawText) = ExtractText(bytes, ct);
        var text = CleanText(rawText);

        if (text.Length < MinTextLength)
        {
            throw new ResumeParseException(
                "Very little text could be read from this PDF. It looks scanned or image-only. " +
                "Please upload a text-based PDF, for example one exported from Word or Google Docs.");
        }

        return new ResumeParseResult(Path.GetFileName(file.FileName), pageCount, text.Length, text);
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await using var stream = file.OpenReadStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    private static (int PageCount, string Text) ExtractText(byte[] bytes, CancellationToken ct)
    {
        try
        {
            using var document = PdfDocument.Open(bytes);

            if (document.NumberOfPages > MaxPages)
            {
                throw new ResumeParseException(
                    $"The PDF has {document.NumberOfPages} pages. Resumes can be at most {MaxPages} pages.");
            }

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                AppendPageText(page, text);
                text.Append("\n\n");
            }

            return (document.NumberOfPages, text.ToString());
        }
        catch (PdfDocumentEncryptedException ex)
        {
            throw new ResumeParseException(
                "This PDF is password-protected. Please remove the password and upload it again.", ex);
        }
        catch (Exception ex) when (ex is not ResumeParseException and not OperationCanceledException)
        {
            throw new ResumeParseException(
                "This PDF appears to be damaged and couldn't be read. Please re-export it and try again.", ex);
        }
    }

    // Groups words into blocks and orders them by layout, so a two-column resume reads
    // column by column instead of interleaving lines from both columns.
    private static void AppendPageText(Page page, StringBuilder text)
    {
        var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
        var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var orderedBlocks = UnsupervisedReadingOrderDetector.Instance.Get(blocks);

        foreach (var block in orderedBlocks)
        {
            text.Append(block.Text).Append("\n\n");
        }
    }

    internal static string CleanText(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        text = NonPrintableRegex().Replace(text, string.Empty);
        text = HorizontalWhitespaceRegex().Replace(text, " ");
        text = SpaceAroundNewlineRegex().Replace(text, "\n");
        text = BlankLinesRegex().Replace(text, "\n\n");
        return text.Trim();
    }

    // Control characters other than newline, plus invisible format characters (zero-width spaces, BOMs, soft hyphens).
    [GeneratedRegex(@"[\p{Cc}\p{Cf}-[\n\t]]")]
    private static partial Regex NonPrintableRegex();

    [GeneratedRegex(@"[\t\p{Zs}]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@" *\n *")]
    private static partial Regex SpaceAroundNewlineRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLinesRegex();
}
