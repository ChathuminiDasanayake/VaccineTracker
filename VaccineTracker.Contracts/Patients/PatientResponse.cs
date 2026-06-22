namespace VaccineTracker.Contracts.Patients;

public sealed record PatientResponse(
    Guid Id,
    Guid HospitalId,
    string PatientNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? NationalIdNumber,
    string? Email,
    string? PhoneNumber,
    string? StreetAddress,
    string? City,
    string? PostalCode,
    string? Country,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsEmployee,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
