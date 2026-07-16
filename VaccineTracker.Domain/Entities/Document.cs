using VaccineTracker.Domain.Common;
using VaccineTracker.Domain.Enums;

namespace VaccineTracker.Domain.Entities;

public sealed class Document : BaseAuditableEntity
{
    public Guid HospitalId { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? VaccinationRecordId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public DocumentType Type { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public DocumentProcessingStatus ProcessingStatus { get; set; } =
        DocumentProcessingStatus.Uploaded;

    public Hospital Hospital { get; set; } = null!;

    public Patient? Patient { get; set; }

    public VaccinationRecord? VaccinationRecord { get; set; }
}
