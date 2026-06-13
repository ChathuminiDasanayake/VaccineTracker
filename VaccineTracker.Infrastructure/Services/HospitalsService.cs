using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.Hospitals;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class HospitalsService : IHospitalsService
{
    private readonly VaccineTrackerDbContext _dbContext;

    public HospitalsService(VaccineTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<HospitalResponse>> GetHospitalsAsync(
        GetHospitalsRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.Hospitals
            .AsNoTracking()
            .Where(hospital => !hospital.IsDeleted);

        if (request.IsActive.HasValue)
        {
            query = query.Where(hospital => hospital.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(hospital =>
                hospital.Name.Contains(search) ||
                (hospital.RegistrationNumber != null && hospital.RegistrationNumber.Contains(search)) ||
                (hospital.ContactEmail != null && hospital.ContactEmail.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var hospitals = await query
            .OrderBy(hospital => hospital.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(hospital => new HospitalResponse(
                hospital.Id,
                hospital.Name,
                hospital.RegistrationNumber,
                hospital.ContactPhone,
                hospital.ContactEmail,
                hospital.LocationId,
                hospital.OpeningHours,
                hospital.IsActive,
                hospital.CreatedAt,
                hospital.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<HospitalResponse>(
            hospitals,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<HospitalResponse?> GetHospitalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hospital = await _dbContext.Hospitals
            .AsNoTracking()
            .FirstOrDefaultAsync(hospital => hospital.Id == id && !hospital.IsDeleted, cancellationToken);

        return hospital is null ? null : ToResponse(hospital);
    }

    public async Task<HospitalResponse> CreateHospitalAsync(
        CreateHospitalRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospital = new Hospital
        {
            Name = request.Name.Trim(),
            RegistrationNumber = request.RegistrationNumber,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            LocationId = request.LocationId,
            OpeningHours = request.OpeningHours,
            IsActive = true
        };

        _dbContext.Hospitals.Add(hospital);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(hospital);
    }

    public async Task<HospitalResponse?> UpdateHospitalAsync(
        Guid id,
        UpdateHospitalRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospital = await _dbContext.Hospitals
            .FirstOrDefaultAsync(hospital => hospital.Id == id && !hospital.IsDeleted, cancellationToken);

        if (hospital is null)
        {
            return null;
        }

        hospital.Name = request.Name.Trim();
        hospital.RegistrationNumber = request.RegistrationNumber;
        hospital.ContactPhone = request.ContactPhone;
        hospital.ContactEmail = request.ContactEmail;
        hospital.LocationId = request.LocationId;
        hospital.OpeningHours = request.OpeningHours;
        hospital.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(hospital);
    }

    public async Task<bool> ActivateHospitalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await SetHospitalActiveStateAsync(id, true, cancellationToken);
    }

    public async Task<bool> DeactivateHospitalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await SetHospitalActiveStateAsync(id, false, cancellationToken);
    }

    private async Task<bool> SetHospitalActiveStateAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var hospital = await _dbContext.Hospitals
            .FirstOrDefaultAsync(hospital => hospital.Id == id && !hospital.IsDeleted, cancellationToken);

        if (hospital is null)
        {
            return false;
        }

        hospital.IsActive = isActive;
        hospital.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static HospitalResponse ToResponse(Hospital hospital)
    {
        return new HospitalResponse(
            hospital.Id,
            hospital.Name,
            hospital.RegistrationNumber,
            hospital.ContactPhone,
            hospital.ContactEmail,
            hospital.LocationId,
            hospital.OpeningHours,
            hospital.IsActive,
            hospital.CreatedAt,
            hospital.UpdatedAt);
    }
}
