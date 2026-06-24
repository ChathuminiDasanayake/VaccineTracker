using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Patients;

public sealed record UpdatePatientRequest(
    [Required]
    [MaxLength(100)]
    string FirstName,
    [Required]
    [MaxLength(100)]
    string LastName,
    DateOnly DateOfBirth,
    [Required]
    string Gender,
    [MaxLength(100)]
    string? NationalIdNumber,
    [EmailAddress]
    [MaxLength(254)]
    string? Email,
    [Phone]
    [MaxLength(30)]
    string? PhoneNumber,
    [MaxLength(300)]
    string? StreetAddress,
    [MaxLength(100)]
    string? City,
    [MaxLength(20)]
    string? PostalCode,
    [MaxLength(100)]
    string? Country,
    [MaxLength(200)]
    string? EmergencyContactName,
    [Phone]
    [MaxLength(30)]
    string? EmergencyContactPhone,
    bool IsEmployee);
