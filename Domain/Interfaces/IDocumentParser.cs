using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Domain.Interfaces;

public interface IDocumentParser
{
    List<DocumentSection> ParseSections(string rawText);
}
