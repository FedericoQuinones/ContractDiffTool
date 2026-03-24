namespace ContractDiffTool.Domain.Models;

public class ContractDocument
{
    public string FileName { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string RawText { get; set; } = string.Empty;
    public List<DocumentSection> Sections { get; set; } = new();
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}
