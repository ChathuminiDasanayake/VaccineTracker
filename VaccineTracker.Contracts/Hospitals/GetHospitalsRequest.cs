using System.ComponentModel.DataAnnotations;

namespace VaccineTracker.Contracts.Hospitals;

public sealed class GetHospitalsRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    public bool? IsActive { get; init; }

    [MaxLength(200)]
    public string? Search { get; init; }
}
