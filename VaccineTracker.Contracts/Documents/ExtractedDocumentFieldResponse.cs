namespace VaccineTracker.Contracts.Documents;

public record ExtractedDocumentFieldResponse(
    Guid Id,
    string FieldName,
    string? ExtractedValue,
    string? CorrectedValue,
    decimal? Confidence,
    bool IsApproved);
