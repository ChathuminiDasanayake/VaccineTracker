namespace VaccineTracker.Contracts.VaccinationRecords;

public sealed record VaccinationRecordResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid HospitalId,
    Guid VaccineProductId,
    string VaccineProductName,
    string VaccineProductCode,
    Guid VaccineTypeId,
    string VaccineTypeName,
    string VaccineTypeCode,
    Guid? VaccineScheduleItemId,
    int DoseNumber,
    DateOnly AdministeredDate,
    Guid AdministeredByUserId,
    string? BatchNumber,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
