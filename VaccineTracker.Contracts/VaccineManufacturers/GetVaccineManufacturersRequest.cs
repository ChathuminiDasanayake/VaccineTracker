using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.VaccineManufacturers;

public sealed class GetVaccineManufacturersRequest : PaginationRequest
{
    public string? Search { get; init; }

    public string? Status { get; init; }
}
