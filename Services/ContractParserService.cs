using System.Text.RegularExpressions;
using ContractDiffTool.Domain.Interfaces;
using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Services;

public class ContractParserService : IDocumentParser
{
    // Patterns ordered by specificity
    private static readonly (Regex Pattern, SectionType Type, int Level)[] SectionPatterns =
    {
        // "ARTICLE I" or "ARTICLE 1" style
        (new Regex(@"^(ARTICLE|Article)\s+([IVXLCDM]+|\d+)[.:\s\-]*(.*)$", RegexOptions.Compiled), SectionType.Header, 1),

        // "Section 4.2.1 - Title" or "SECTION 4.2 Title"
        (new Regex(@"^(SECTION|Section)\s+(\d+(?:\.\d+)*)[.:\s\-]*(.*)$", RegexOptions.Compiled), SectionType.Clause, 0), // Level computed from number

        // Standalone numbered section "1." or "1.2" or "1.2.3" followed by text
        (new Regex(@"^(\d+(?:\.\d+)+)[.:\s)\-]+(.+)$", RegexOptions.Compiled), SectionType.SubClause, 0),
        (new Regex(@"^(\d+)[.:\s)\-]+([A-Z].{2,})$", RegexOptions.Compiled), SectionType.Clause, 1),

        // ALL CAPS header (at least 3 chars, all uppercase letters/spaces)
        (new Regex(@"^([A-Z][A-Z\s]{2,}[A-Z])[\s:.\-]*$", RegexOptions.Compiled), SectionType.Header, 1),

        // "WHEREAS" / "RECITAL" / "NOW, THEREFORE"
        (new Regex(@"^(WHEREAS|RECITAL|NOW,?\s*THEREFORE)", RegexOptions.Compiled | RegexOptions.IgnoreCase), SectionType.Recital, 1),

        // Signature blocks
        (new Regex(@"^(IN WITNESS WHEREOF|SIGNED|EXECUTED|BY:\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase), SectionType.SignatureBlock, 1),

        // Definition: "Term" means ... or "Term" shall mean
        (new Regex(@"^""([^""]+)""\s+(means|shall mean|refers to)", RegexOptions.Compiled | RegexOptions.IgnoreCase), SectionType.Definition, 2),

        // Lettered items: (a), (b), (i), (ii)
        (new Regex(@"^\s*\(([a-z]|[ivxlcdm]+|\d+)\)\s+", RegexOptions.Compiled), SectionType.NumberedItem, 3),

        // Numbered sub-items: a. b. i. ii.
        (new Regex(@"^\s+([a-z]|[ivxlcdm]+)\.\s+", RegexOptions.Compiled), SectionType.NumberedItem, 3),
    };

    public List<DocumentSection> ParseSections(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<DocumentSection> { CreateFallbackSection(rawText ?? string.Empty) };

        var lines = rawText.Split('\n');
        var sections = new List<DocumentSection>();
        var currentContent = new List<string>();
        string currentHeading = string.Empty;
        string currentNumber = string.Empty;
        var currentType = SectionType.Paragraph;
        int currentLevel = 1;
        int sectionIndex = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                currentContent.Add(string.Empty);
                continue;
            }

            var match = MatchSectionPattern(line);
            if (match.HasValue)
            {
                // Finalize previous section
                if (currentContent.Count > 0 || !string.IsNullOrEmpty(currentHeading))
                {
                    sections.Add(new DocumentSection
                    {
                        Index = sectionIndex++,
                        Type = currentType,
                        Heading = currentHeading,
                        Content = JoinContent(currentContent),
                        Level = currentLevel,
                        SectionNumber = currentNumber,
                        PageNumber = 1
                    });
                }

                currentHeading = match.Value.Heading;
                currentNumber = match.Value.Number;
                currentType = match.Value.Type;
                currentLevel = match.Value.Level;
                currentContent.Clear();

                // If there's trailing text after the heading pattern, start content with it
                if (!string.IsNullOrWhiteSpace(match.Value.TrailingText))
                {
                    currentContent.Add(match.Value.TrailingText);
                }
            }
            else
            {
                currentContent.Add(line);
            }
        }

        // Finalize last section
        if (currentContent.Count > 0 || !string.IsNullOrEmpty(currentHeading))
        {
            sections.Add(new DocumentSection
            {
                Index = sectionIndex,
                Type = currentType,
                Heading = currentHeading,
                Content = JoinContent(currentContent),
                Level = currentLevel,
                SectionNumber = currentNumber,
                PageNumber = 1
            });
        }

        return sections.Count > 0 ? sections : new List<DocumentSection> { CreateFallbackSection(rawText) };
    }

    private static (string Heading, string Number, SectionType Type, int Level, string TrailingText)? MatchSectionPattern(string line)
    {
        var trimmed = line.TrimStart();

        foreach (var (pattern, type, baseLevel) in SectionPatterns)
        {
            var m = pattern.Match(trimmed);
            if (!m.Success) continue;

            string heading;
            string number = string.Empty;
            string trailing = string.Empty;
            int level = baseLevel;

            switch (type)
            {
                case SectionType.Header when m.Groups.Count >= 4:
                    number = m.Groups[2].Value;
                    heading = string.IsNullOrWhiteSpace(m.Groups[3].Value)
                        ? $"{m.Groups[1].Value} {number}"
                        : $"{m.Groups[1].Value} {number} - {m.Groups[3].Value.Trim()}";
                    break;

                case SectionType.Clause when m.Groups.Count >= 4:
                    number = m.Groups[2].Value;
                    level = number.Count(c => c == '.') + 1;
                    heading = string.IsNullOrWhiteSpace(m.Groups[3].Value)
                        ? $"Section {number}"
                        : $"Section {number} - {m.Groups[3].Value.Trim()}";
                    break;

                case SectionType.SubClause when m.Groups.Count >= 3:
                    number = m.Groups[1].Value;
                    level = number.Count(c => c == '.') + 1;
                    heading = $"{number} {m.Groups[2].Value.Trim()}";
                    trailing = string.Empty;
                    break;

                case SectionType.Clause when m.Groups.Count >= 3:
                    number = m.Groups[1].Value;
                    heading = $"{number}. {m.Groups[2].Value.Trim()}";
                    break;

                case SectionType.Definition:
                    heading = $"\"{m.Groups[1].Value}\"";
                    trailing = trimmed;
                    break;

                default:
                    heading = trimmed;
                    break;
            }

            return (heading, number, type, level, trailing);
        }

        return null;
    }

    private static string JoinContent(List<string> lines)
    {
        var text = string.Join("\n", lines).Trim();
        // Collapse multiple blank lines into one
        return Regex.Replace(text, @"\n{3,}", "\n\n");
    }

    private static DocumentSection CreateFallbackSection(string text)
    {
        return new DocumentSection
        {
            Index = 0,
            Type = SectionType.Paragraph,
            Heading = "Document Content",
            Content = text,
            Level = 1,
            SectionNumber = string.Empty,
            PageNumber = 1
        };
    }
}
