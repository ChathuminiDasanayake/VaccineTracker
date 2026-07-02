using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.VaccineProducts;

public sealed class GetVaccineProductsRequest : PaginationRequest
{
    public Guid? VaccineTypeId { get; init; }

    public Guid? ManufacturerId { get; init; }

    public string? Search { get; init; }

    public string? Status { get; init; }
}
