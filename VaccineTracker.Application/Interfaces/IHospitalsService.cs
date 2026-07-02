using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Hospitals;

namespace VaccineTracker.Application.Interfaces;

public interface IHospitalsService
{
    Task<PagedResponse<HospitalResponse>> GetHospitalsAsync(
        GetHospitalsRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalResponse> GetHospitalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HospitalResponse> CreateHospitalAsync(
        CreateHospitalRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalResponse> UpdateHospitalAsync(
        Guid id,
        UpdateHospitalRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalResponse> ActivateHospitalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HospitalResponse> DeactivateHospitalAsync(Guid id, CancellationToken cancellationToken = default);
}
