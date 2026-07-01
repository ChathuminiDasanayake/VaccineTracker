using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineManufacturers;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class VaccineManufacturersService : IVaccineManufacturersService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ILogger<VaccineManufacturersService> _logger;

    public VaccineManufacturersService(
        VaccineTrackerDbContext dbContext,
        ILogger<VaccineManufacturersService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResponse<VaccineManufacturerResponse>> GetManufacturersAsync(
        GetVaccineManufacturersRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.VaccineManufacturers
            .AsNoTracking()
            .Where(manufacturer => !manufacturer.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(manufacturer =>
                manufacturer.Name.Contains(search) ||
                manufacturer.Code.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(manufacturer => manufacturer.Status == status);
        }
        else
        {
            query = query.Where(manufacturer => manufacturer.Status == EntityStatus.Active);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var manufacturers = await query
            .OrderBy(manufacturer => manufacturer.Name)
            .ThenBy(manufacturer => manufacturer.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(manufacturer => new VaccineManufacturerResponse(
                manufacturer.Id,
                manufacturer.Name,
                manufacturer.Code,
                manufacturer.Description,
                manufacturer.Status.ToString(),
                manufacturer.CreatedAt,
                manufacturer.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<VaccineManufacturerResponse>(
            manufacturers,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<VaccineManufacturerResponse> GetManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
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

        return ToResponse(manufacturer);
    }

    public async Task<VaccineManufacturerResponse> CreateManufacturerAsync(
        CreateVaccineManufacturerRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.VaccineManufacturers
            .AnyAsync(
                manufacturer => !manufacturer.IsDeleted &&
                    manufacturer.Code == code,
                cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"A vaccine manufacturer with code '{code}' already exists.");
        }

        var manufacturer = new VaccineManufacturer
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = TrimOrNull(request.Description),
            Status = EntityStatus.Active
        };

        _dbContext.VaccineManufacturers.Add(manufacturer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine manufacturer {ManufacturerId} with code {Code} created.",
            manufacturer.Id,
            manufacturer.Code);

        return ToResponse(manufacturer);
    }

    public async Task<VaccineManufacturerResponse> UpdateManufacturerAsync(
        Guid manufacturerId,
        UpdateVaccineManufacturerRequest request,
        CancellationToken cancellationToken = default)
    {
        var manufacturer = await GetTrackedManufacturerAsync(
            manufacturerId,
            cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.VaccineManufacturers
            .AnyAsync(
                existingManufacturer =>
                    existingManufacturer.Id != manufacturerId &&
                    !existingManufacturer.IsDeleted &&
                    existingManufacturer.Code == code,
                cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"A vaccine manufacturer with code '{code}' already exists.");
        }

        manufacturer.Name = request.Name.Trim();
        manufacturer.Code = code;
        manufacturer.Description = TrimOrNull(request.Description);
        manufacturer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine manufacturer {ManufacturerId} updated.",
            manufacturer.Id);

        return ToResponse(manufacturer);
    }

    public async Task<VaccineManufacturerResponse> ActivateManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
    {
        return await SetManufacturerStatusAsync(
            manufacturerId,
            EntityStatus.Active,
            cancellationToken);
    }

    public async Task<VaccineManufacturerResponse> DeactivateManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
    {
        return await SetManufacturerStatusAsync(
            manufacturerId,
            EntityStatus.Inactive,
            cancellationToken);
    }

    private async Task<VaccineManufacturerResponse> SetManufacturerStatusAsync(
        Guid manufacturerId,
        EntityStatus status,
        CancellationToken cancellationToken)
    {
        var manufacturer = await GetTrackedManufacturerAsync(
            manufacturerId,
            cancellationToken);

        if (manufacturer.Status == status)
        {
            throw new BusinessRuleException(
                $"Vaccine manufacturer is already {status}.");
        }

        manufacturer.Status = status;
        manufacturer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine manufacturer {ManufacturerId} status changed to {Status}.",
            manufacturer.Id,
            manufacturer.Status);

        return ToResponse(manufacturer);
    }

    private async Task<VaccineManufacturer> GetTrackedManufacturerAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken)
    {
        var manufacturer = await _dbContext.VaccineManufacturers
            .FirstOrDefaultAsync(
                manufacturer => manufacturer.Id == manufacturerId &&
                    !manufacturer.IsDeleted,
                cancellationToken);

        if (manufacturer is null)
        {
            throw new NotFoundException("Vaccine manufacturer", manufacturerId);
        }

        return manufacturer;
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

    private static VaccineManufacturerResponse ToResponse(VaccineManufacturer manufacturer)
    {
        return new VaccineManufacturerResponse(
            manufacturer.Id,
            manufacturer.Name,
            manufacturer.Code,
            manufacturer.Description,
            manufacturer.Status.ToString(),
            manufacturer.CreatedAt,
            manufacturer.UpdatedAt);
    }
}
