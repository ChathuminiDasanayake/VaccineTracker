namespace VaccineTracker.Contracts.Hospitals;

public sealed record CreateHospitalRequest(
    string Name,
    string? RegistrationNumber,
    string? ContactPhone,
    string? ContactEmail,
    Guid? LocationId,
    string? OpeningHours);
