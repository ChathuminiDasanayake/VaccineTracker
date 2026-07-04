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
    DateTime CreatedAt,
    DateTime? UpdatedAt);
