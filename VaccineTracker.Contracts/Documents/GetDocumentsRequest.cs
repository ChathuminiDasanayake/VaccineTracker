using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.Documents;

public sealed class GetDocumentsRequest : PaginationRequest
{
    public Guid? PatientId { get; init; }

    public Guid? VaccinationRecordId { get; init; }

    public string? Type { get; init; }

    public string? Status { get; init; }
}
