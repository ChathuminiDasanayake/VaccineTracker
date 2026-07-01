namespace VaccineTracker.Contracts.VaccineManufacturers;

public sealed record VaccineManufacturerResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
