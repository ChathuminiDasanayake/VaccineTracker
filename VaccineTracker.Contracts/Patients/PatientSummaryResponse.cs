namespace VaccineTracker.Contracts.Patients;

public sealed record PatientSummaryResponse(
    Guid Id,
    string PatientNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Status);
