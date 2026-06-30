using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Vaccines;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class VaccinesService : IVaccinesService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ILogger<VaccinesService> _logger;

    public VaccinesService(
        VaccineTrackerDbContext dbContext,
        ILogger<VaccinesService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResponse<VaccineResponse>> GetVaccinesAsync(
        GetVaccinesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.Vaccines
            .AsNoTracking()
            .Include(vaccine => vaccine.Manufacturer)
            .Where(vaccine => !vaccine.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(vaccine =>
                vaccine.Name.Contains(search) ||
                vaccine.Code.Contains(search) ||
                vaccine.DiseaseTarget.Contains(search) ||
                vaccine.Manufacturer.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(vaccine => vaccine.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var vaccines = await query
            .OrderBy(vaccine => vaccine.Name)
            .ThenBy(vaccine => vaccine.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(vaccine => new VaccineResponse(
                vaccine.Id,
                vaccine.Name,
                vaccine.Code,
                vaccine.ManufacturerId,
                vaccine.Manufacturer.Name,
                vaccine.DiseaseTarget,
                vaccine.Description,
                vaccine.Status.ToString(),
                vaccine.CreatedAt,
                vaccine.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<VaccineResponse>(
            vaccines,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<VaccineResponse> CreateVaccineAsync(
        CreateVaccineRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var manufacturer = await _dbContext.VaccineManufacturers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                manufacturer =>
                    manufacturer.Id == request.ManufacturerId &&
                    manufacturer.Status == EntityStatus.Active &&
                    !manufacturer.IsDeleted,
                cancellationToken);

        if (manufacturer is null)
        {
            throw new NotFoundException("Active vaccine manufacturer", request.ManufacturerId);
        }

        var codeExists = await _dbContext.Vaccines
            .AnyAsync(
                vaccine => !vaccine.IsDeleted &&
                    vaccine.Code == code,
                cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"A vaccine with code '{code}' already exists.");
        }

        var vaccine = new Vaccine
        {
            Name = request.Name.Trim(),
            Code = code,
            ManufacturerId = request.ManufacturerId,
            DiseaseTarget = request.DiseaseTarget.Trim(),
            Description = TrimOrNull(request.Description),
            Status = EntityStatus.Active
        };

        _dbContext.Vaccines.Add(vaccine);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine {VaccineId} with code {Code} created.",
            vaccine.Id,
            vaccine.Code);

        return new VaccineResponse(
            vaccine.Id,
            vaccine.Name,
            vaccine.Code,
            vaccine.ManufacturerId,
            manufacturer.Name,
            vaccine.DiseaseTarget,
            vaccine.Description,
            vaccine.Status.ToString(),
            vaccine.CreatedAt,
            vaccine.UpdatedAt);
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
}
