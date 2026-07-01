using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.VaccineManufacturers;

public sealed record UpdateVaccineManufacturerRequest(
    [Required]
    [MaxLength(200)]
    string Name,
    [Required]
    [MaxLength(50)]
    string Code,
    [MaxLength(500)]
    string? Description);
