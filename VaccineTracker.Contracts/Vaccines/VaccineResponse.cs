namespace VaccineTracker.Contracts.Vaccines;

public sealed record VaccineResponse(
    Guid Id,
    string Name,
    string Code,
    Guid ManufacturerId,
    string ManufacturerName,
    string DiseaseTarget,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
