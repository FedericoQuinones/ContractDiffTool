using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace ContractDiffTool.Pages;

public class ResultsModel : PageModel
{
    private readonly IMemoryCache _cache;
    private readonly IReportGenerator _reportGenerator;

    public ResultsModel(IMemoryCache cache, IReportGenerator reportGenerator)
    {
        _cache = cache;
        _reportGenerator = reportGenerator;
    }

    public DiffResult? Result { get; set; }
    public string ReportHtml { get; set; } = string.Empty;

    public IActionResult OnGet(string? id)
    {
        if (string.IsNullOrEmpty(id) || !_cache.TryGetValue<DiffResult>(id, out var result) || result == null)
        {
            return RedirectToPage("Index");
        }

        Result = result;
        ReportHtml = _reportGenerator.GenerateHtmlReport(result);
        return Page();
    }

    public IActionResult OnGetDownload(string? id)
    {
        if (string.IsNullOrEmpty(id) || !_cache.TryGetValue<DiffResult>(id, out var result) || result == null)
        {
            return RedirectToPage("Index");
        }

        var html = _reportGenerator.GenerateHtmlReport(result);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        return File(bytes, "text/html", $"contract-diff-{id[..8]}.html");
    }
}
