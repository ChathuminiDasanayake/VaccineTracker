using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineProducts;

namespace VaccineTracker.Application.Interfaces;

public interface IVaccineProductsService
{
    Task<PagedResponse<VaccineProductResponse>> GetProductsAsync(
        GetVaccineProductsRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineProductResponse> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<VaccineProductResponse> CreateProductAsync(
        CreateVaccineProductRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineProductResponse> UpdateProductAsync(
        Guid productId,
        UpdateVaccineProductRequest request,
        CancellationToken cancellationToken = default);

    Task<VaccineProductResponse> ActivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<VaccineProductResponse> DeactivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
