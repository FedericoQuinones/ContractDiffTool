using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Domain.Interfaces;

public interface IDiffEngine
{
    DiffResult Compare(ContractDocument original, ContractDocument revised);
}
