using VaccineTracker.Domain.Common;

namespace VaccineTracker.Domain.Entities;

public sealed class DocumentExtraction : BaseAuditableEntity
{
    public Guid DocumentId { get; set; }

    public string ModelId { get; set; } = string.Empty;

    public string RawResultJson { get; set; } = string.Empty;

    public decimal? OverallConfidence { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }

    public Document Document { get; set; } = null!;

    public ICollection<ExtractedDocumentField> Fields { get; set; } = [];
}
