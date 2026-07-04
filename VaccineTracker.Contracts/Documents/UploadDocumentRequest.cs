namespace VaccineTracker.Contracts.Documents;

public sealed class UploadDocumentRequest
{
    public Guid? PatientId { get; init; }

    public Guid? VaccinationRecordId { get; init; }

    public string Type { get; init; } = string.Empty;
}
