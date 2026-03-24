namespace ContractDiffTool.Domain.Models;

public class DiffResult
{
    public ContractDocument OriginalDocument { get; set; } = null!;
    public ContractDocument RevisedDocument { get; set; } = null!;
    public List<SectionChange> Changes { get; set; } = new();
    public DiffSummary Summary { get; set; } = new();
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public string ComparisonId { get; set; } = Guid.NewGuid().ToString("N");
}
