using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Vaccines;

public sealed record UpdateVaccineRequest(
    [Required]
    [MaxLength(200)]
    string Name,
    [Required]
    [MaxLength(50)]
    string Code,
    [Required]
    [MaxLength(200)]
    string DiseaseTarget,
    [MaxLength(500)]
    string? Description);
