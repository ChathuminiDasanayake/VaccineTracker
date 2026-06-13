namespace VaccineTracker.Contracts.Hospitals;

public sealed record HospitalResponse(
    Guid Id,
    string Name,
    string? RegistrationNumber,
    string? ContactPhone,
    string? ContactEmail,
    Guid? LocationId,
    string? OpeningHours,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
