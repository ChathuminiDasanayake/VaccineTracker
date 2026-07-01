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

        var query = _dbContext.VaccineTypes
            .AsNoTracking()
            .Where(vaccineType => !vaccineType.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(vaccineType =>
                vaccineType.Name.Contains(search) ||
                vaccineType.Code.Contains(search) ||
                vaccineType.DiseaseTarget.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(vaccineType => vaccineType.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var vaccines = await query
            .OrderBy(vaccineType => vaccineType.Name)
            .ThenBy(vaccineType => vaccineType.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(vaccineType => new VaccineResponse(
                vaccineType.Id,
                vaccineType.Name,
                vaccineType.Code,
                vaccineType.DiseaseTarget,
                vaccineType.Description,
                vaccineType.Status.ToString(),
                vaccineType.CreatedAt,
                vaccineType.UpdatedAt))
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

        var codeExists = await _dbContext.VaccineTypes
            .AnyAsync(
                vaccineType => !vaccineType.IsDeleted &&
                    vaccineType.Code == code,
                cancellationToken);

        if (codeExists)
        {
            throw new ConflictException(
                $"A vaccine type with code '{code}' already exists.");
        }

        var vaccineType = new VaccineType
        {
            Name = request.Name.Trim(),
            Code = code,
            DiseaseTarget = request.DiseaseTarget.Trim(),
            Description = TrimOrNull(request.Description),
            Status = EntityStatus.Active
        };

        _dbContext.VaccineTypes.Add(vaccineType);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccine type {VaccineTypeId} with code {Code} created.",
            vaccineType.Id,
            vaccineType.Code);

        return new VaccineResponse(
            vaccineType.Id,
            vaccineType.Name,
            vaccineType.Code,
            vaccineType.DiseaseTarget,
            vaccineType.Description,
            vaccineType.Status.ToString(),
            vaccineType.CreatedAt,
            vaccineType.UpdatedAt);
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
