namespace VaccineTracker.Contracts.VaccineProducts;

public sealed record VaccineProductResponse(
    Guid Id,
    Guid VaccineTypeId,
    string VaccineTypeName,
    string VaccineTypeCode,
    Guid ManufacturerId,
    string ManufacturerName,
    string ManufacturerCode,
    string Name,
    string Code,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
