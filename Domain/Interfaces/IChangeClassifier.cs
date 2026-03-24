using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Domain.Interfaces;

public interface IChangeClassifier
{
    (ChangeSeverity Severity, ChangeCategory Category, string Description) Classify(SectionChange change);
}
