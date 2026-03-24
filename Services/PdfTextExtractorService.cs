using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;
using UglyToad.PdfPig;

namespace ContractDiffTool.Services;

public class PdfTextExtractorService : IPdfTextExtractor
{
    private readonly IDocumentParser _parser;
    private readonly ILogger<PdfTextExtractorService> _logger;

    public PdfTextExtractorService(IDocumentParser parser, ILogger<PdfTextExtractorService> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    public ContractDocument ExtractDocument(Stream pdfStream, string fileName)
    {
        var textParts = new List<string>();
        int totalPages;

        using var document = PdfDocument.Open(pdfStream);
        totalPages = document.NumberOfPages;

        for (int i = 1; i <= totalPages; i++)
        {
            var page = document.GetPage(i);
            var pageText = string.Join(" ", page.GetWords().Select(w => w.Text));

            if (string.IsNullOrWhiteSpace(pageText))
            {
                pageText = page.Text ?? string.Empty;
            }

            textParts.Add(pageText.Trim());
        }

        var rawText = string.Join("\n\n", textParts.Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(rawText))
        {
            _logger.LogWarning("No text extracted from {FileName}. The PDF may be scanned/image-based.", fileName);
        }

        var sections = _parser.ParseSections(rawText);

        return new ContractDocument
        {
            FileName = fileName,
            TotalPages = totalPages,
            RawText = rawText,
            Sections = sections
        };
    }
}
