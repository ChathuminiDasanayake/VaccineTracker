using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Hospitals;

public sealed record CreateHospitalRequest(
    [Required]
    [MaxLength(200)]
    string Name,
    [MaxLength(100)]
    string? RegistrationNumber,
    [Phone]
    [MaxLength(30)]
    string? ContactPhone,
    [EmailAddress]
    [MaxLength(254)]
    string? ContactEmail,
    Guid? LocationId,
    [MaxLength(500)]
    string? OpeningHours);
