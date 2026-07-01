using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Vaccines;

namespace VaccineTracker.Application.Interfaces;

public interface IVaccinesService
{
    Task<PagedResponse<VaccineResponse>> GetVaccinesAsync(
        GetVaccinesRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineResponse> CreateVaccineAsync(
        CreateVaccineRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineResponse> UpdateVaccineAsync(
        Guid vaccineTypeId,
        UpdateVaccineRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineResponse> ActivateVaccineAsync(
        Guid vaccineTypeId,
        CancellationToken cancellationToken = default);

    Task<VaccineResponse> DeactivateVaccineAsync(
        Guid vaccineTypeId,
        CancellationToken cancellationToken = default);
}
