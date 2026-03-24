namespace ContractDiffTool.Domain.Models;

public class InlineChange
{
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public int Position { get; set; }
}
