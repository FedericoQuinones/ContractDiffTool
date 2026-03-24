using ContractDiffTool.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContractDiffTool.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class DiffController : ControllerBase
{
    private readonly IPdfTextExtractor _extractor;
    private readonly IDiffEngine _diffEngine;
    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger<DiffController> _logger;

    public DiffController(
        IPdfTextExtractor extractor,
        IDiffEngine diffEngine,
        IReportGenerator reportGenerator,
        ILogger<DiffController> logger)
    {
        _extractor = extractor;
        _diffEngine = diffEngine;
        _reportGenerator = reportGenerator;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", version = "1.0.0", tool = "Contract Diff Tool" });
    }

    [HttpPost("compare")]
    public IActionResult Compare(IFormFile original, IFormFile revised)
    {
        var validationError = ValidateFiles(original, revised);
        if (validationError != null) return validationError;

        try
        {
            using var origStream = original.OpenReadStream();
            using var revStream = revised.OpenReadStream();

            var origDoc = _extractor.ExtractDocument(origStream, original.FileName);
            var revDoc = _extractor.ExtractDocument(revStream, revised.FileName);
            var result = _diffEngine.Compare(origDoc, revDoc);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing documents");
            return StatusCode(500, new { error = "Failed to compare documents. Ensure both files are valid text-based PDFs." });
        }
    }

    [HttpPost("compare/report")]
    public IActionResult CompareReport(IFormFile original, IFormFile revised)
    {
        var validationError = ValidateFiles(original, revised);
        if (validationError != null) return validationError;

        try
        {
            using var origStream = original.OpenReadStream();
            using var revStream = revised.OpenReadStream();

            var origDoc = _extractor.ExtractDocument(origStream, original.FileName);
            var revDoc = _extractor.ExtractDocument(revStream, revised.FileName);
            var result = _diffEngine.Compare(origDoc, revDoc);
            var html = _reportGenerator.GenerateHtmlReport(result);

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diff report");
            return StatusCode(500, new { error = "Failed to generate report. Ensure both files are valid text-based PDFs." });
        }
    }

    private BadRequestObjectResult? ValidateFiles(IFormFile? original, IFormFile? revised)
    {
        if (original == null || revised == null)
            return BadRequest(new { error = "Both 'original' and 'revised' PDF files are required." });

        if (!original.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Original file must be a PDF." });

        if (!revised.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Revised file must be a PDF." });

        return null;
    }
}
