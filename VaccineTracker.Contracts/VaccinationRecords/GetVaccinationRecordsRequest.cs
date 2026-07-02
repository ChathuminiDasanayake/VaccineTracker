using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.VaccinationRecords;

public sealed class GetVaccinationRecordsRequest : PaginationRequest
{
    public Guid? PatientId { get; init; }

    public Guid? VaccineProductId { get; init; }

    public Guid? VaccineScheduleItemId { get; init; }

    public DateOnly? AdministeredFrom { get; init; }

    public DateOnly? AdministeredTo { get; init; }

    public string? Status { get; init; }
}
