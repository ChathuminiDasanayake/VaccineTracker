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
}
