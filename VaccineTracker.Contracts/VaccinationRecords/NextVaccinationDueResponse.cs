namespace VaccineTracker.Contracts.VaccinationRecords;

public sealed record NextVaccinationDueResponse(
    Guid PatientId,
    string PatientNumber,
    Guid VaccineTypeId,
    string VaccineTypeName,
    string VaccineTypeCode,
    Guid VaccineScheduleItemId,
    string TargetGroup,
    int DoseNumber,
    DateOnly DueDate,
    int DaysUntilDue,
    bool IsOverdue,
    string? Description);
