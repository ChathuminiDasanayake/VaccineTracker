using VaccineTracker.Contracts.Documents;

namespace VaccineTracker.Application.Interfaces;

public interface IDocumentIntelligenceService
{
    Task<DocumentExtractionResponse> AnalyzeDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentExtractionResponse>> GetDocumentExtractionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
