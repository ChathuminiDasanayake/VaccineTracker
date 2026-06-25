using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Contracts.Patients;

public sealed class PatientSearchRequest : PaginationRequest
{
    public string? PatientNumber { get; init; }

    public string? Name { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public string? Status { get; init; }
}
