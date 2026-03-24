using System.Text.RegularExpressions;
using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Services;

public class ChangeClassifierService : IChangeClassifier
{
    private static readonly Regex FinancialPattern = new(
        @"(\$[\d,]+(\.\d{2})?|USD|EUR|GBP|\d+(\.\d+)?%|\b(price|fee|compensation|penalty|rate|cost|payment|amount|salary|bonus|revenue|profit)\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DatePattern = new(
        @"(\d{1,2}/\d{1,2}/\d{2,4}|\d{1,2}-\d{1,2}-\d{2,4}|\b(January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2}|" +
        @"\b(deadline|expiration|effective\s+date|term|renewal|termination\s+date|commencement|duration|notice\s+period)\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LegalTermsPattern = new(
        @"\b(indemnif|liabilit|warrant|breach|terminat|arbitrat|jurisdict|governing\s+law|force\s+majeure|confidential|" +
        @"non-compete|non-disclosure|severability|waiver|intellectual\s+property|limitation\s+of\s+liability|" +
        @"representations?\s+and\s+warranties|default|remedy|injunctive|negligence|damages|covenant)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DefinitionPattern = new(
        @"("".+""\s+(means|shall\s+mean|refers\s+to)|defined\s+term|\bdefinition\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public (ChangeSeverity Severity, ChangeCategory Category, string Description) Classify(SectionChange change)
    {
        var changedText = GetChangedText(change);
        var sectionLabel = GetSectionLabel(change);

        // Critical: Financial changes
        if (FinancialPattern.IsMatch(changedText))
        {
            var details = ExtractMatchContext(FinancialPattern, changedText);
            return (ChangeSeverity.Critical, ChangeCategory.Financial,
                $"Financial terms {change.Type.ToString().ToLower()} in {sectionLabel}: {details}");
        }

        // Critical: Date/timing changes
        if (DatePattern.IsMatch(changedText))
        {
            var details = ExtractMatchContext(DatePattern, changedText);
            return (ChangeSeverity.Critical, ChangeCategory.DatesTiming,
                $"Date/timing {change.Type.ToString().ToLower()} in {sectionLabel}: {details}");
        }

        // Critical: Legal terms
        if (LegalTermsPattern.IsMatch(changedText))
        {
            var details = ExtractMatchContext(LegalTermsPattern, changedText);
            return (ChangeSeverity.Critical, ChangeCategory.LegalTerms,
                $"Legal terms {change.Type.ToString().ToLower()} in {sectionLabel}: {details}");
        }

        // Major: Definition changes
        if (change.OriginalSection?.Type == SectionType.Definition ||
            change.RevisedSection?.Type == SectionType.Definition ||
            DefinitionPattern.IsMatch(changedText))
        {
            return (ChangeSeverity.Major, ChangeCategory.Definitions,
                $"Definition {change.Type.ToString().ToLower()} in {sectionLabel}");
        }

        // Major: Entire section added/removed
        if (change.Type == Domain.Models.ChangeType.Added || change.Type == Domain.Models.ChangeType.Removed)
        {
            return (ChangeSeverity.Major, ChangeCategory.Structural,
                $"Section {change.Type.ToString().ToLower()}: {sectionLabel}");
        }

        // Major: Clause/SubClause modification
        if ((change.OriginalSection?.Type is SectionType.Clause or SectionType.SubClause ||
             change.RevisedSection?.Type is SectionType.Clause or SectionType.SubClause) &&
            change.Type == Domain.Models.ChangeType.Modified)
        {
            return (ChangeSeverity.Major, ChangeCategory.ClauseModification,
                $"Clause modified in {sectionLabel}");
        }

        // Minor: Formatting-only changes (whitespace/case)
        if (change.Type == Domain.Models.ChangeType.Modified && IsFormattingOnly(change))
        {
            return (ChangeSeverity.Minor, ChangeCategory.Formatting,
                $"Formatting change in {sectionLabel}");
        }

        // Minor: Typo-level changes
        if (change.Type == Domain.Models.ChangeType.Modified && IsTypoLevel(change))
        {
            return (ChangeSeverity.Minor, ChangeCategory.Typo,
                $"Minor text change in {sectionLabel}");
        }

        // Default: Major clause modification
        return (ChangeSeverity.Major, ChangeCategory.ClauseModification,
            $"Content {change.Type.ToString().ToLower()} in {sectionLabel}");
    }

    private static string GetChangedText(SectionChange change)
    {
        var parts = new List<string>();

        if (change.OriginalSection != null)
            parts.Add(change.OriginalSection.FullText);
        if (change.RevisedSection != null)
            parts.Add(change.RevisedSection.FullText);

        foreach (var inline in change.InlineChanges)
        {
            if (!string.IsNullOrEmpty(inline.OldText)) parts.Add(inline.OldText);
            if (!string.IsNullOrEmpty(inline.NewText)) parts.Add(inline.NewText);
        }

        return string.Join(" ", parts);
    }

    private static string GetSectionLabel(SectionChange change)
    {
        var section = change.RevisedSection ?? change.OriginalSection;
        if (section == null) return "unknown section";

        if (!string.IsNullOrWhiteSpace(section.Heading))
            return $"\"{section.Heading}\"";
        if (!string.IsNullOrWhiteSpace(section.SectionNumber))
            return $"Section {section.SectionNumber}";

        return $"section #{section.Index + 1}";
    }

    private static string ExtractMatchContext(Regex pattern, string text)
    {
        var matches = pattern.Matches(text);
        var terms = matches.Take(3).Select(m => m.Value.Trim()).Distinct();
        return string.Join(", ", terms);
    }

    private static bool IsFormattingOnly(SectionChange change)
    {
        if (change.OriginalSection == null || change.RevisedSection == null) return false;

        var orig = Regex.Replace(change.OriginalSection.FullText, @"\s+", " ").Trim().ToLowerInvariant();
        var rev = Regex.Replace(change.RevisedSection.FullText, @"\s+", " ").Trim().ToLowerInvariant();
        return orig == rev;
    }

    private static bool IsTypoLevel(SectionChange change)
    {
        if (change.InlineChanges.Count == 0) return false;

        int totalEditDistance = 0;
        foreach (var inline in change.InlineChanges)
        {
            totalEditDistance += LevenshteinDistance(inline.OldText, inline.NewText);
        }

        return totalEditDistance <= 3;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var costs = new int[b.Length + 1];
        for (int i = 0; i <= b.Length; i++) costs[i] = i;

        for (int i = 1; i <= a.Length; i++)
        {
            int prev = i - 1;
            costs[0] = i;

            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                int current = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    prev + cost);
                prev = costs[j];
                costs[j] = current;
            }
        }

        return costs[b.Length];
    }
}
