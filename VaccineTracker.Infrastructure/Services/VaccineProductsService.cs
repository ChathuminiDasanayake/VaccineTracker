using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineProducts;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class VaccineProductsService : IVaccineProductsService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<VaccineProductsService> _logger;

    public VaccineProductsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<VaccineProductsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResponse<VaccineProductResponse>> GetProductsAsync(
        GetVaccineProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.VaccineProducts
            .AsNoTracking()
            .Where(product => !product.IsDeleted);

        if (request.VaccineTypeId.HasValue)
        {
            query = query.Where(product =>
                product.VaccineTypeId == request.VaccineTypeId.Value);
        }

        if (request.ManufacturerId.HasValue)
        {
            query = query.Where(product =>
                product.ManufacturerId == request.ManufacturerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(product =>
                product.Name.Contains(search) ||
                product.Code.Contains(search) ||
                product.VaccineType.Name.Contains(search) ||
                product.VaccineType.Code.Contains(search) ||
                product.Manufacturer.Name.Contains(search) ||
                product.Manufacturer.Code.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(product => product.Status == status);
        }
        else
        {
            query = query.Where(product => product.Status == EntityStatus.Active);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(product => product.VaccineType.Name)
            .ThenBy(product => product.Manufacturer.Name)
            .ThenBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new VaccineProductResponse(
                product.Id,
                product.VaccineTypeId,
                product.VaccineType.Name,
                product.VaccineType.Code,
                product.ManufacturerId,
                product.Manufacturer.Name,
                product.Manufacturer.Code,
                product.Name,
                product.Code,
                product.Description,
                product.Status.ToString(),
                product.CreatedAt,
                product.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<VaccineProductResponse>(
            products,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<VaccineProductResponse> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.VaccineProducts
            .AsNoTracking()
            .Include(product => product.VaccineType)
            .Include(product => product.Manufacturer)
            .FirstOrDefaultAsync(
                product => product.Id == productId &&
                    !product.IsDeleted,
                cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Vaccine product", productId);
        }

        return ToResponse(product);
    }

    public async Task<VaccineProductResponse> CreateProductAsync(
        CreateVaccineProductRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureVaccineTypeCanBeUsedAsync(
            request.VaccineTypeId,
            cancellationToken);

        await EnsureManufacturerCanBeUsedAsync(
            request.ManufacturerId,
            cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();

        await EnsureCodeIsUniqueAsync(
            code,
            ignoredProductId: null,
            cancellationToken);

        var product = new VaccineProduct
        {
            VaccineTypeId = request.VaccineTypeId,
            ManufacturerId = request.ManufacturerId,
            Name = request.Name.Trim(),
            Code = code,
            Description = TrimOrNull(request.Description),
            Status = EntityStatus.Active,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.VaccineProducts.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine product {ProductId} with code {Code} created.",
            product.Id,
            product.Code);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    public async Task<VaccineProductResponse> UpdateProductAsync(
        Guid productId,
        UpdateVaccineProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await GetTrackedProductAsync(productId, cancellationToken);

        await EnsureVaccineTypeCanBeUsedAsync(
            request.VaccineTypeId,
            cancellationToken);

        await EnsureManufacturerCanBeUsedAsync(
            request.ManufacturerId,
            cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();

        await EnsureCodeIsUniqueAsync(
            code,
            ignoredProductId: productId,
            cancellationToken);

        product.VaccineTypeId = request.VaccineTypeId;
        product.ManufacturerId = request.ManufacturerId;
        product.Name = request.Name.Trim();
        product.Code = code;
        product.Description = TrimOrNull(request.Description);
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine product {ProductId} updated.",
            product.Id);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    public async Task<VaccineProductResponse> ActivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await SetProductStatusAsync(
            productId,
            EntityStatus.Active,
            cancellationToken);
    }

    public async Task<VaccineProductResponse> DeactivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await SetProductStatusAsync(
            productId,
            EntityStatus.Inactive,
            cancellationToken);
    }

    private async Task<VaccineProductResponse> SetProductStatusAsync(
        Guid productId,
        EntityStatus status,
        CancellationToken cancellationToken)
    {
        var product = await GetTrackedProductAsync(productId, cancellationToken);

        if (product.Status == status)
        {
            throw new BusinessRuleException(
                $"Vaccine product is already {status}.");
        }

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine product {ProductId} status changed to {Status}.",
            product.Id,
            product.Status);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    private async Task<VaccineProduct> GetTrackedProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.VaccineProducts
            .FirstOrDefaultAsync(
                product => product.Id == productId &&
                    !product.IsDeleted,
                cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Vaccine product", productId);
        }

        return product;
    }

    private async Task EnsureVaccineTypeCanBeUsedAsync(
        Guid vaccineTypeId,
        CancellationToken cancellationToken)
    {
        var vaccineType = await _dbContext.VaccineTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vaccineType => vaccineType.Id == vaccineTypeId &&
                    !vaccineType.IsDeleted,
                cancellationToken);

        if (vaccineType is null)
        {
            throw new NotFoundException("Vaccine type", vaccineTypeId);
        }

        if (vaccineType.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccine product cannot use an inactive vaccine type.");
        }
    }

    private async Task EnsureManufacturerCanBeUsedAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _dbContext.VaccineManufacturers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                manufacturer => manufacturer.Id == manufacturerId &&
                    !manufacturer.IsDeleted,
                cancellationToken);

        if (manufacturer is null)
        {
            throw new NotFoundException("Vaccine manufacturer", manufacturerId);
        }

        if (manufacturer.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccine product cannot use an inactive manufacturer.");
        }
    }

    private async Task EnsureCodeIsUniqueAsync(
        string code,
        Guid? ignoredProductId,
        CancellationToken cancellationToken)
    {
        var codeExists = await _dbContext.VaccineProducts
            .AnyAsync(
                product =>
                    !product.IsDeleted &&
                    product.Code == code &&
                    (!ignoredProductId.HasValue ||
                        product.Id != ignoredProductId.Value),
                cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"A vaccine product with code '{code}' already exists.");
        }
    }

    private static bool TryParseEntityStatus(string status, out EntityStatus value)
    {
        return Enum.TryParse(status, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static VaccineProductResponse ToResponse(VaccineProduct product)
    {
        return new VaccineProductResponse(
            product.Id,
            product.VaccineTypeId,
            product.VaccineType.Name,
            product.VaccineType.Code,
            product.ManufacturerId,
            product.Manufacturer.Name,
            product.Manufacturer.Code,
            product.Name,
            product.Code,
            product.Description,
            product.Status.ToString(),
            product.CreatedAt,
            product.UpdatedAt);
    }
}
