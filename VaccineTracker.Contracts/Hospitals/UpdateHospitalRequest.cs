namespace VaccineTracker.Contracts.Hospitals;

public sealed record UpdateHospitalRequest(
    string Name,
    string? RegistrationNumber,
    string? ContactPhone,
    string? ContactEmail,
    Guid? LocationId,
    string? OpeningHours);
