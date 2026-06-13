using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Interfaces;
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

    public async Task<IReadOnlyList<HospitalResponse>> GetHospitalsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Hospitals
            .AsNoTracking()
            .Where(hospital => !hospital.IsDeleted)
            .OrderBy(hospital => hospital.Name)
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
