using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Hospitals;

public sealed record UpdateHospitalRequest(
    [property: Required]
    [property: MaxLength(200)]
    string Name,
    [property: MaxLength(100)]
    string? RegistrationNumber,
    [property: Phone]
    [property: MaxLength(30)]
    string? ContactPhone,
    [property: EmailAddress]
    [property: MaxLength(254)]
    string? ContactEmail,
    Guid? LocationId,
    [property: MaxLength(500)]
    string? OpeningHours);
