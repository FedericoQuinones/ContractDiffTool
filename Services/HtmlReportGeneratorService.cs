using System.Text;
using System.Web;
using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Services;

public class HtmlReportGeneratorService : IReportGenerator
{
    public string GenerateHtmlReport(DiffResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>Contract Diff Report — {Encode(result.OriginalDocument.FileName)} vs {Encode(result.RevisedDocument.FileName)}</title>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine("<div class=\"report-header\">");
        sb.AppendLine("<h1>Contract Diff Report</h1>");
        sb.AppendLine("<div class=\"meta\">");
        sb.AppendLine($"<span><strong>Original:</strong> {Encode(result.OriginalDocument.FileName)} ({result.OriginalDocument.TotalPages} pages)</span>");
        sb.AppendLine($"<span><strong>Revised:</strong> {Encode(result.RevisedDocument.FileName)} ({result.RevisedDocument.TotalPages} pages)</span>");
        sb.AppendLine($"<span><strong>Compared:</strong> {result.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC</span>");
        sb.AppendLine($"<span><strong>ID:</strong> {result.ComparisonId}</span>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Summary cards
        RenderSummary(sb, result.Summary);

        // Filter bar
        RenderFilterBar(sb);

        // Changes list
        sb.AppendLine("<div class=\"changes-container\">");
        if (result.Changes.Count == 0)
        {
            sb.AppendLine("<div class=\"no-changes\">No differences found between the two documents.</div>");
        }
        else
        {
            foreach (var change in result.Changes)
            {
                RenderChange(sb, change);
            }
        }
        sb.AppendLine("</div>");

        sb.AppendLine(GetScript());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void RenderSummary(StringBuilder sb, DiffSummary summary)
    {
        sb.AppendLine("<div class=\"summary-section\">");
        sb.AppendLine("<h2>Executive Summary</h2>");
        sb.AppendLine("<div class=\"summary-cards\">");

        RenderCard(sb, "Total Changes", summary.TotalChanges.ToString(), "total");
        RenderCard(sb, "Critical", summary.CriticalChanges.ToString(), "critical");
        RenderCard(sb, "Major", summary.MajorChanges.ToString(), "major");
        RenderCard(sb, "Minor", summary.MinorChanges.ToString(), "minor");

        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"summary-cards\">");
        RenderCard(sb, "Added", summary.Additions.ToString(), "added");
        RenderCard(sb, "Removed", summary.Removals.ToString(), "removed");
        RenderCard(sb, "Modified", summary.Modifications.ToString(), "modified");
        RenderCard(sb, "Moved", summary.Moves.ToString(), "moved");
        sb.AppendLine("</div>");

        if (summary.ByCategory.Count > 0)
        {
            sb.AppendLine("<div class=\"category-breakdown\">");
            sb.AppendLine("<h3>By Category</h3>");
            sb.AppendLine("<table><thead><tr><th>Category</th><th>Count</th></tr></thead><tbody>");
            foreach (var (cat, count) in summary.ByCategory.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"<tr><td>{FormatCategory(cat)}</td><td>{count}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
    }

    private static void RenderCard(StringBuilder sb, string label, string value, string cssClass)
    {
        sb.AppendLine($"<div class=\"card {cssClass}\">");
        sb.AppendLine($"<div class=\"card-value\">{value}</div>");
        sb.AppendLine($"<div class=\"card-label\">{label}</div>");
        sb.AppendLine("</div>");
    }

    private static void RenderFilterBar(StringBuilder sb)
    {
        sb.AppendLine("<div class=\"filter-bar\">");
        sb.AppendLine("<span class=\"filter-label\">Filter:</span>");
        sb.AppendLine("<button class=\"filter-btn active\" data-filter=\"all\">All</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Critical\">Critical</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Major\">Major</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Minor\">Minor</button>");
        sb.AppendLine("<span class=\"filter-sep\">|</span>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Added\">Added</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Removed\">Removed</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Modified\">Modified</button>");
        sb.AppendLine("<button class=\"filter-btn\" data-filter=\"Moved\">Moved</button>");
        sb.AppendLine("</div>");
    }

    private static void RenderChange(StringBuilder sb, SectionChange change)
    {
        var severityClass = change.Severity.ToString().ToLower();
        var typeClass = change.Type.ToString().ToLower();

        sb.AppendLine($"<div class=\"change-card {severityClass}\" data-severity=\"{change.Severity}\" data-type=\"{change.Type}\">");

        // Badges
        sb.AppendLine("<div class=\"change-header\">");
        sb.AppendLine($"<span class=\"badge severity-{severityClass}\">{change.Severity}</span>");
        sb.AppendLine($"<span class=\"badge type-{typeClass}\">{change.Type}</span>");
        sb.AppendLine($"<span class=\"badge cat\">{FormatCategory(change.Category)}</span>");
        sb.AppendLine("</div>");

        // Description
        sb.AppendLine($"<p class=\"change-desc\">{Encode(change.Description)}</p>");

        // Diff content
        if (change.Type == Domain.Models.ChangeType.Modified || change.Type == Domain.Models.ChangeType.Moved)
        {
            RenderSideBySide(sb, change);
        }
        else if (change.Type == Domain.Models.ChangeType.Added && change.RevisedSection != null)
        {
            sb.AppendLine("<div class=\"diff-panel added-panel\">");
            sb.AppendLine($"<div class=\"panel-title\">Added Content</div>");
            sb.AppendLine($"<pre>{Encode(change.RevisedSection.FullText)}</pre>");
            sb.AppendLine("</div>");
        }
        else if (change.Type == Domain.Models.ChangeType.Removed && change.OriginalSection != null)
        {
            sb.AppendLine("<div class=\"diff-panel removed-panel\">");
            sb.AppendLine($"<div class=\"panel-title\">Removed Content</div>");
            sb.AppendLine($"<pre>{Encode(change.OriginalSection.FullText)}</pre>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
    }

    private static void RenderSideBySide(StringBuilder sb, SectionChange change)
    {
        sb.AppendLine("<div class=\"side-by-side\">");

        // Original
        sb.AppendLine("<div class=\"diff-panel original-panel\">");
        sb.AppendLine("<div class=\"panel-title\">Original</div>");
        sb.AppendLine("<pre>");
        if (change.OriginalSection != null)
        {
            var origLines = change.OriginalSection.FullText.Split('\n');
            var changedPositions = new HashSet<int>(change.InlineChanges.Select(ic => ic.Position));

            for (int i = 0; i < origLines.Length; i++)
            {
                if (changedPositions.Contains(i))
                {
                    var inline = change.InlineChanges.FirstOrDefault(ic => ic.Position == i);
                    sb.AppendLine($"<span class=\"line-deleted\">{Encode(inline?.OldText ?? origLines[i])}</span>");
                }
                else
                {
                    sb.AppendLine(Encode(origLines[i]));
                }
            }
        }
        sb.AppendLine("</pre>");
        sb.AppendLine("</div>");

        // Revised
        sb.AppendLine("<div class=\"diff-panel revised-panel\">");
        sb.AppendLine("<div class=\"panel-title\">Revised</div>");
        sb.AppendLine("<pre>");
        if (change.RevisedSection != null)
        {
            var revLines = change.RevisedSection.FullText.Split('\n');
            var changedPositions = new HashSet<int>(change.InlineChanges.Select(ic => ic.Position));

            for (int i = 0; i < revLines.Length; i++)
            {
                if (changedPositions.Contains(i))
                {
                    var inline = change.InlineChanges.FirstOrDefault(ic => ic.Position == i);
                    sb.AppendLine($"<span class=\"line-inserted\">{Encode(inline?.NewText ?? revLines[i])}</span>");
                }
                else
                {
                    sb.AppendLine(Encode(revLines[i]));
                }
            }
        }
        sb.AppendLine("</pre>");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
    }

    private static string FormatCategory(ChangeCategory cat)
    {
        return cat switch
        {
            ChangeCategory.DatesTiming => "Dates & Timing",
            ChangeCategory.LegalTerms => "Legal Terms",
            ChangeCategory.ClauseModification => "Clause Modification",
            _ => cat.ToString()
        };
    }

    private static string Encode(string text) => HttpUtility.HtmlEncode(text ?? string.Empty);

    private static string GetStyles()
    {
        return """
        <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8fafc; color: #1e293b; line-height: 1.6; }

            .report-header { background: linear-gradient(135deg, #1e293b 0%, #334155 100%); color: white; padding: 2rem 2.5rem; }
            .report-header h1 { font-size: 1.75rem; margin-bottom: 0.75rem; }
            .report-header .meta { display: flex; flex-wrap: wrap; gap: 1.5rem; font-size: 0.875rem; opacity: 0.9; }

            .summary-section { padding: 1.5rem 2.5rem; }
            .summary-section h2 { font-size: 1.25rem; margin-bottom: 1rem; color: #334155; }
            .summary-section h3 { font-size: 1rem; margin-bottom: 0.5rem; color: #475569; }
            .summary-cards { display: flex; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap; }

            .card { background: white; border-radius: 8px; padding: 1rem 1.5rem; min-width: 120px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #94a3b8; text-align: center; }
            .card-value { font-size: 2rem; font-weight: 700; }
            .card-label { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; color: #64748b; }

            .card.total { border-left-color: #3b82f6; }
            .card.total .card-value { color: #3b82f6; }
            .card.critical { border-left-color: #ef4444; }
            .card.critical .card-value { color: #ef4444; }
            .card.major { border-left-color: #f97316; }
            .card.major .card-value { color: #f97316; }
            .card.minor { border-left-color: #6366f1; }
            .card.minor .card-value { color: #6366f1; }
            .card.added { border-left-color: #22c55e; }
            .card.added .card-value { color: #22c55e; }
            .card.removed { border-left-color: #ef4444; }
            .card.removed .card-value { color: #ef4444; }
            .card.modified { border-left-color: #eab308; }
            .card.modified .card-value { color: #eab308; }
            .card.moved { border-left-color: #8b5cf6; }
            .card.moved .card-value { color: #8b5cf6; }

            .category-breakdown { background: white; border-radius: 8px; padding: 1rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
            .category-breakdown table { width: 100%; border-collapse: collapse; }
            .category-breakdown th, .category-breakdown td { padding: 0.5rem 1rem; text-align: left; border-bottom: 1px solid #e2e8f0; }
            .category-breakdown th { font-weight: 600; color: #475569; font-size: 0.85rem; text-transform: uppercase; }

            .filter-bar { padding: 1rem 2.5rem; display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; background: white; border-top: 1px solid #e2e8f0; border-bottom: 1px solid #e2e8f0; position: sticky; top: 0; z-index: 10; }
            .filter-label { font-weight: 600; color: #475569; margin-right: 0.5rem; }
            .filter-sep { color: #cbd5e1; }
            .filter-btn { padding: 0.35rem 0.9rem; border: 1px solid #cbd5e1; border-radius: 20px; background: white; cursor: pointer; font-size: 0.8rem; transition: all 0.15s; }
            .filter-btn:hover { background: #f1f5f9; }
            .filter-btn.active { background: #1e293b; color: white; border-color: #1e293b; }

            .changes-container { padding: 1.5rem 2.5rem; }
            .no-changes { text-align: center; padding: 3rem; color: #64748b; font-size: 1.1rem; }

            .change-card { background: white; border-radius: 8px; padding: 1.25rem 1.5rem; margin-bottom: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-left: 4px solid #94a3b8; }
            .change-card.critical { border-left-color: #ef4444; }
            .change-card.major { border-left-color: #f97316; }
            .change-card.minor { border-left-color: #6366f1; }

            .change-header { display: flex; gap: 0.5rem; margin-bottom: 0.75rem; flex-wrap: wrap; }
            .badge { padding: 0.2rem 0.6rem; border-radius: 12px; font-size: 0.7rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.03em; }
            .severity-critical { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; }
            .severity-major { background: #fff7ed; color: #ea580c; border: 1px solid #fed7aa; }
            .severity-minor { background: #eef2ff; color: #4f46e5; border: 1px solid #c7d2fe; }
            .type-added { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }
            .type-removed { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; }
            .type-modified { background: #fefce8; color: #ca8a04; border: 1px solid #fde68a; }
            .type-moved { background: #faf5ff; color: #7c3aed; border: 1px solid #ddd6fe; }
            .badge.cat { background: #f8fafc; color: #475569; border: 1px solid #e2e8f0; }

            .change-desc { color: #475569; margin-bottom: 1rem; font-size: 0.95rem; }

            .side-by-side { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
            .diff-panel { border: 1px solid #e2e8f0; border-radius: 6px; overflow: hidden; }
            .panel-title { background: #f1f5f9; padding: 0.5rem 1rem; font-weight: 600; font-size: 0.85rem; color: #475569; border-bottom: 1px solid #e2e8f0; }
            .diff-panel pre { padding: 1rem; font-size: 0.85rem; white-space: pre-wrap; word-break: break-word; overflow-x: auto; max-height: 400px; overflow-y: auto; line-height: 1.7; }
            .added-panel { border-color: #bbf7d0; }
            .added-panel .panel-title { background: #f0fdf4; color: #16a34a; }
            .added-panel pre { background: #f0fdf4; }
            .removed-panel { border-color: #fecaca; }
            .removed-panel .panel-title { background: #fef2f2; color: #dc2626; }
            .removed-panel pre { background: #fef2f2; }

            .line-deleted { background: #fecaca; display: inline; padding: 1px 2px; border-radius: 2px; }
            .line-inserted { background: #bbf7d0; display: inline; padding: 1px 2px; border-radius: 2px; }

            @media (max-width: 768px) {
                .side-by-side { grid-template-columns: 1fr; }
                .report-header, .summary-section, .changes-container, .filter-bar { padding-left: 1rem; padding-right: 1rem; }
            }

            @media print {
                .filter-bar { display: none; }
                .change-card { break-inside: avoid; }
                body { background: white; }
            }
        </style>
        """;
    }

    private static string GetScript()
    {
        return """
        <script>
        document.addEventListener('DOMContentLoaded', function() {
            const buttons = document.querySelectorAll('.filter-btn');
            const cards = document.querySelectorAll('.change-card');

            buttons.forEach(btn => {
                btn.addEventListener('click', function() {
                    const filter = this.dataset.filter;

                    buttons.forEach(b => b.classList.remove('active'));
                    this.classList.add('active');

                    cards.forEach(card => {
                        if (filter === 'all') {
                            card.style.display = '';
                        } else {
                            const severity = card.dataset.severity;
                            const type = card.dataset.type;
                            card.style.display = (severity === filter || type === filter) ? '' : 'none';
                        }
                    });
                });
            });
        });
        </script>
        """;
    }
}
