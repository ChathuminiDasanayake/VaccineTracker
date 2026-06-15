namespace VaccineTracker.Contracts.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    Guid? HospitalId,
    IReadOnlyCollection<string> Roles,
    string? Gender,
    string? PhoneNumber,
    string Status,
    string? EmployeeId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
