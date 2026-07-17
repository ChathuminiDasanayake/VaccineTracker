using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities;

public sealed class ExtractedDocumentField : BaseAuditableEntity
{
    public Guid DocumentExtractionId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? ExtractedValue { get; set; }

    public string? CorrectedValue { get; set; }

    public decimal? Confidence { get; set; }

    public bool IsApproved { get; set; }

    public DocumentExtraction DocumentExtraction { get; set; } = null!;
}
