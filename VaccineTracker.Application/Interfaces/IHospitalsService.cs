using VaccineTracker.Contracts.Hospitals;

namespace VaccineTracker.Application.Interfaces;

public interface IHospitalsService
{
    Task<IReadOnlyList<HospitalResponse>> GetHospitalsAsync(CancellationToken cancellationToken = default);

    Task<HospitalResponse?> GetHospitalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HospitalResponse> CreateHospitalAsync(
        CreateHospitalRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalResponse?> UpdateHospitalAsync(
        Guid id,
        UpdateHospitalRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateHospitalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeactivateHospitalAsync(Guid id, CancellationToken cancellationToken = default);
}
