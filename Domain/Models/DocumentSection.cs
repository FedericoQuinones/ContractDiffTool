namespace ContractDiffTool.Domain.Models;

public class DocumentSection
{
    public int Index { get; set; }
    public int PageNumber { get; set; }
    public SectionType Type { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public string SectionNumber { get; set; } = string.Empty;

    public string FullText => string.IsNullOrWhiteSpace(Heading)
        ? Content
        : $"{Heading}\n{Content}";

    public string NormalizedHeading =>
        Heading.Trim().ToUpperInvariant();
}
