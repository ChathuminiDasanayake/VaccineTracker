using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.VaccineScheduleItems;

public sealed class GetVaccineScheduleItemsRequest : PaginationRequest
{
    public Guid? VaccineTypeId { get; init; }

    public string? TargetGroup { get; init; }

    public string? Status { get; init; }

    public string? Search { get; init; }
}
