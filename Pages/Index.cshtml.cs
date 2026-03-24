using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace ContractDiffTool.Pages;

public class IndexModel : PageModel
{
    private readonly IPdfTextExtractor _extractor;
    private readonly IDiffEngine _diffEngine;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPdfTextExtractor extractor,
        IDiffEngine diffEngine,
        IMemoryCache cache,
        ILogger<IndexModel> logger)
    {
        _extractor = extractor;
        _diffEngine = diffEngine;
        _cache = cache;
        _logger = logger;
    }

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public IActionResult OnPost(IFormFile? original, IFormFile? revised)
    {
        if (original == null || revised == null)
        {
            ErrorMessage = "Please upload both the original and revised PDF files.";
            return Page();
        }

        if (!original.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !revised.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Both files must be PDFs.";
            return Page();
        }

        try
        {
            using var origStream = original.OpenReadStream();
            using var revStream = revised.OpenReadStream();

            var origDoc = _extractor.ExtractDocument(origStream, original.FileName);
            var revDoc = _extractor.ExtractDocument(revStream, revised.FileName);
            var result = _diffEngine.Compare(origDoc, revDoc);

            _cache.Set(result.ComparisonId, result, TimeSpan.FromMinutes(30));

            return RedirectToPage("Results", new { id = result.ComparisonId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDFs");
            ErrorMessage = "Failed to process the PDF files. Please ensure they are valid text-based PDFs (not scanned images).";
            return Page();
        }
    }
}
