using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Domain.Interfaces;

public interface IReportGenerator
{
    string GenerateHtmlReport(DiffResult result);
}
