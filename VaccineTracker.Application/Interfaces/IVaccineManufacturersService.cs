using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineManufacturers;

namespace VaccineTracker.Application.Interfaces;

public interface IVaccineManufacturersService
{
    Task<PagedResponse<VaccineManufacturerResponse>> GetManufacturersAsync(
        GetVaccineManufacturersRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineManufacturerResponse> GetManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default);

    Task<VaccineManufacturerResponse> CreateManufacturerAsync(
        CreateVaccineManufacturerRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineManufacturerResponse> UpdateManufacturerAsync(
        Guid manufacturerId,
        UpdateVaccineManufacturerRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineManufacturerResponse> ActivateManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default);

    Task<VaccineManufacturerResponse> DeactivateManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default);
}
