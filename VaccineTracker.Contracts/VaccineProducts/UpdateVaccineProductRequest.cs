using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.VaccineProducts;

public sealed record UpdateVaccineProductRequest(
    [Required]
    Guid VaccineTypeId,
    [Required]
    Guid ManufacturerId,
    [Required]
    [MaxLength(200)]
    string Name,
    [Required]
    [MaxLength(50)]
    string Code,
    [MaxLength(500)]
    string? Description);
