using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace ContractDiffTool.Services;

public class DiffEngineService : IDiffEngine
{
    private readonly IChangeClassifier _classifier;

    public DiffEngineService(IChangeClassifier classifier)
    {
        _classifier = classifier;
    }

    public DiffResult Compare(ContractDocument original, ContractDocument revised)
    {
        var changes = new List<SectionChange>();
        var matchedOriginal = new HashSet<int>();
        var matchedRevised = new HashSet<int>();
        int changeIndex = 0;

        // Pass 1: Exact heading match
        foreach (var origSection in original.Sections)
        {
            foreach (var revSection in revised.Sections)
            {
                if (matchedRevised.Contains(revSection.Index)) continue;

                if (AreHeadingsEqual(origSection, revSection))
                {
                    matchedOriginal.Add(origSection.Index);
                    matchedRevised.Add(revSection.Index);

                    if (!AreContentsEqual(origSection, revSection))
                    {
                        var change = CreateModifiedChange(changeIndex++, origSection, revSection);
                        changes.Add(change);
                    }
                    break;
                }
            }
        }

        // Pass 1.5: Position-based matching for sections without headings
        foreach (var origSection in original.Sections.Where(s => !matchedOriginal.Contains(s.Index)))
        {
            var positionalMatch = revised.Sections
                .FirstOrDefault(s => !matchedRevised.Contains(s.Index)
                    && s.Index == origSection.Index);

            if (positionalMatch != null)
            {
                matchedOriginal.Add(origSection.Index);
                matchedRevised.Add(positionalMatch.Index);

                if (!AreFullTextEqual(origSection, positionalMatch))
                {
                    var change = CreateModifiedChange(changeIndex++, origSection, positionalMatch);
                    changes.Add(change);
                }
            }
        }

        // Pass 2: Fuzzy content match for remaining unmatched sections
        foreach (var origSection in original.Sections.Where(s => !matchedOriginal.Contains(s.Index)))
        {
            DocumentSection? bestMatch = null;
            double bestSimilarity = 0;

            foreach (var revSection in revised.Sections.Where(s => !matchedRevised.Contains(s.Index)))
            {
                var similarity = ComputeSimilarity(origSection.FullText, revSection.FullText);
                if (similarity > bestSimilarity && similarity > 0.5)
                {
                    bestSimilarity = similarity;
                    bestMatch = revSection;
                }
            }

            if (bestMatch != null)
            {
                matchedOriginal.Add(origSection.Index);
                matchedRevised.Add(bestMatch.Index);

                // Skip if content is actually identical
                if (AreFullTextEqual(origSection, bestMatch))
                    continue;

                var changeType = origSection.Index != bestMatch.Index
                    ? Domain.Models.ChangeType.Moved
                    : Domain.Models.ChangeType.Modified;

                var change = new SectionChange
                {
                    Index = changeIndex++,
                    Type = changeType,
                    OriginalSection = origSection,
                    RevisedSection = bestMatch,
                    InlineChanges = ComputeInlineChanges(origSection.FullText, bestMatch.FullText)
                };
                ClassifyAndDescribe(change);
                changes.Add(change);
            }
        }

        // Pass 3: Remaining unmatched = Removed / Added
        foreach (var origSection in original.Sections.Where(s => !matchedOriginal.Contains(s.Index)))
        {
            var change = new SectionChange
            {
                Index = changeIndex++,
                Type = Domain.Models.ChangeType.Removed,
                OriginalSection = origSection,
                RevisedSection = null
            };
            ClassifyAndDescribe(change);
            changes.Add(change);
        }

        foreach (var revSection in revised.Sections.Where(s => !matchedRevised.Contains(s.Index)))
        {
            var change = new SectionChange
            {
                Index = changeIndex++,
                Type = Domain.Models.ChangeType.Added,
                OriginalSection = null,
                RevisedSection = revSection
            };
            ClassifyAndDescribe(change);
            changes.Add(change);
        }

        // Sort changes: Critical first, then Major, then Minor
        changes = changes
            .OrderBy(c => c.Severity)
            .ThenBy(c => c.Type)
            .ToList();

        // Re-index
        for (int i = 0; i < changes.Count; i++)
            changes[i].Index = i;

        var summary = BuildSummary(changes);

        return new DiffResult
        {
            OriginalDocument = original,
            RevisedDocument = revised,
            Changes = changes,
            Summary = summary
        };
    }

    private SectionChange CreateModifiedChange(int index, DocumentSection orig, DocumentSection rev)
    {
        var change = new SectionChange
        {
            Index = index,
            Type = Domain.Models.ChangeType.Modified,
            OriginalSection = orig,
            RevisedSection = rev,
            InlineChanges = ComputeInlineChanges(orig.FullText, rev.FullText)
        };
        ClassifyAndDescribe(change);
        return change;
    }

    private void ClassifyAndDescribe(SectionChange change)
    {
        var (severity, category, description) = _classifier.Classify(change);
        change.Severity = severity;
        change.Category = category;
        change.Description = description;
    }

    private static List<InlineChange> ComputeInlineChanges(string oldText, string newText)
    {
        var differ = new Differ();
        var builder = new SideBySideDiffBuilder(differ);
        var model = builder.BuildDiffModel(oldText, newText);
        var inlineChanges = new List<InlineChange>();

        var oldLines = model.OldText.Lines;
        var newLines = model.NewText.Lines;
        int maxLines = Math.Max(oldLines.Count, newLines.Count);

        for (int i = 0; i < maxLines; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;

            bool oldChanged = oldLine != null && oldLine.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged && oldLine.Type != DiffPlex.DiffBuilder.Model.ChangeType.Imaginary;
            bool newChanged = newLine != null && newLine.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged && newLine.Type != DiffPlex.DiffBuilder.Model.ChangeType.Imaginary;

            if (oldChanged || newChanged)
            {
                inlineChanges.Add(new InlineChange
                {
                    OldText = oldLine?.Text ?? string.Empty,
                    NewText = newLine?.Text ?? string.Empty,
                    Position = i
                });
            }
        }

        return inlineChanges;
    }

    private static double ComputeSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

        var differ = new Differ();
        var diff = differ.CreateDiffs(a, b, false, false, new DiffPlex.Chunkers.CharacterChunker());

        int totalLength = Math.Max(a.Length, b.Length);
        int changedChars = 0;

        foreach (var block in diff.DiffBlocks)
        {
            changedChars += block.DeleteCountA + block.InsertCountB;
        }

        return 1.0 - ((double)changedChars / totalLength);
    }

    private static bool AreHeadingsEqual(DocumentSection a, DocumentSection b)
    {
        // Both empty headings — can't match by heading alone
        if (string.IsNullOrWhiteSpace(a.Heading) && string.IsNullOrWhiteSpace(b.Heading))
            return false;

        // One empty, one not — not a match
        if (string.IsNullOrWhiteSpace(a.Heading) || string.IsNullOrWhiteSpace(b.Heading))
            return false;

        return a.NormalizedHeading == b.NormalizedHeading;
    }

    private static bool AreContentsEqual(DocumentSection a, DocumentSection b)
    {
        return string.Equals(a.Content.Trim(), b.Content.Trim(), StringComparison.Ordinal);
    }

    private static bool AreFullTextEqual(DocumentSection a, DocumentSection b)
    {
        return string.Equals(a.FullText.Trim(), b.FullText.Trim(), StringComparison.Ordinal);
    }

    private static DiffSummary BuildSummary(List<SectionChange> changes)
    {
        var summary = new DiffSummary
        {
            TotalChanges = changes.Count,
            Additions = changes.Count(c => c.Type == Domain.Models.ChangeType.Added),
            Removals = changes.Count(c => c.Type == Domain.Models.ChangeType.Removed),
            Modifications = changes.Count(c => c.Type == Domain.Models.ChangeType.Modified),
            Moves = changes.Count(c => c.Type == Domain.Models.ChangeType.Moved),
            CriticalChanges = changes.Count(c => c.Severity == ChangeSeverity.Critical),
            MajorChanges = changes.Count(c => c.Severity == ChangeSeverity.Major),
            MinorChanges = changes.Count(c => c.Severity == ChangeSeverity.Minor)
        };

        foreach (ChangeCategory cat in Enum.GetValues<ChangeCategory>())
        {
            var count = changes.Count(c => c.Category == cat);
            if (count > 0)
                summary.ByCategory[cat] = count;
        }

        return summary;
    }
}
