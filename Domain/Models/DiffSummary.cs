namespace ContractDiffTool.Domain.Models;

public class DiffSummary
{
    public int TotalChanges { get; set; }
    public int Additions { get; set; }
    public int Removals { get; set; }
    public int Modifications { get; set; }
    public int Moves { get; set; }
    public int CriticalChanges { get; set; }
    public int MajorChanges { get; set; }
    public int MinorChanges { get; set; }
    public Dictionary<ChangeCategory, int> ByCategory { get; set; } = new();
}
