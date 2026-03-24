using ContractDiffTool.Domain.Models;

namespace ContractDiffTool.Domain.Interfaces;

public interface IPdfTextExtractor
{
    ContractDocument ExtractDocument(Stream pdfStream, string fileName);
}
