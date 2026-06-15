using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Users;

public sealed record CreateHospitalUserRequest(
    [Required]
    [MaxLength(100)]
    string Username,
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    string Email,
    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    string Password,
    [Required]
    [MaxLength(100)]
    string FirstName,
    [Required]
    [MaxLength(100)]
    string LastName,
    Guid? HospitalId,
    [Required]
    string Role,
    string? Gender,
    [Phone]
    [MaxLength(30)]
    string? PhoneNumber,
    [MaxLength(100)]
    string? EmployeeId);
