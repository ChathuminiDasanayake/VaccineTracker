namespace VaccineTracker.Contracts.Patients;

public sealed record PatientPortalAccessResponse(
    Guid Id,
    Guid PatientId,
    Guid UserId,
    DateTime CreatedAt);
