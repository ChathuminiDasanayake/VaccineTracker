namespace VaccineTracker.Contracts.Documents;

public record DocumentExtractionResponse(
    Guid Id,
    Guid DocumentId,
    string ModelId,
    decimal? OverallConfidence,
    DateTimeOffset ProcessedAt,
    IReadOnlyList<ExtractedDocumentFieldResponse> Fields);
