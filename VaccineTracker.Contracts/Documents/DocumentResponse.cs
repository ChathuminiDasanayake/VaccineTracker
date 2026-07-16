namespace VaccineTracker.Contracts.Documents;

public record DocumentResponse(
    Guid Id,
    Guid HospitalId,
    Guid? PatientId,
    Guid? VaccinationRecordId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    string Type,
    string Status,
    string ProcessingStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
