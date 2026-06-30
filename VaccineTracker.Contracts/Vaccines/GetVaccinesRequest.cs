using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.Vaccines;

public sealed class GetVaccinesRequest : PaginationRequest
{
    public string? Search { get; init; }

    public string? Status { get; init; }
}
