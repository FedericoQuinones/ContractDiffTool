namespace ContractDiffTool.Domain.Models;

public class SectionChange
{
    public int Index { get; set; }
    public ChangeType Type { get; set; }
    public ChangeSeverity Severity { get; set; }
    public ChangeCategory Category { get; set; }
    public DocumentSection? OriginalSection { get; set; }
    public DocumentSection? RevisedSection { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<InlineChange> InlineChanges { get; set; } = new();
}
