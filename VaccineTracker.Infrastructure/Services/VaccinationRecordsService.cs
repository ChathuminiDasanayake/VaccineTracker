using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccinationRecords;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class VaccinationRecordsService : IVaccinationRecordsService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<VaccinationRecordsService> _logger;

    public VaccinationRecordsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<VaccinationRecordsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResponse<VaccinationRecordResponse>> GetRecordsAsync(
        GetVaccinationRecordsRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.VaccinationRecords
            .AsNoTracking()
            .Where(record =>
                record.HospitalId == hospitalId &&
                !record.IsDeleted);

        if (request.PatientId.HasValue)
        {
            query = query.Where(record => record.PatientId == request.PatientId.Value);
        }

        if (request.VaccineProductId.HasValue)
        {
            query = query.Where(record => record.VaccineProductId == request.VaccineProductId.Value);
        }

        if (request.VaccineScheduleItemId.HasValue)
        {
            query = query.Where(record => record.VaccineScheduleItemId == request.VaccineScheduleItemId.Value);
        }

        if (request.AdministeredFrom.HasValue)
        {
            query = query.Where(record => record.AdministeredDate >= request.AdministeredFrom.Value);
        }

        if (request.AdministeredTo.HasValue)
        {
            query = query.Where(record => record.AdministeredDate <= request.AdministeredTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseRecordStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Vaccination record status '{request.Status}' is invalid.");
            }

            query = query.Where(record => record.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(record => record.AdministeredDate)
            .ThenByDescending(record => record.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(record => new VaccinationRecordResponse(
                record.Id,
                record.PatientId,
                record.Patient.PatientNumber,
                record.Patient.FirstName + " " + record.Patient.LastName,
                record.HospitalId,
                record.VaccineProductId,
                record.VaccineProduct.Name,
                record.VaccineProduct.Code,
                record.VaccineProduct.VaccineTypeId,
                record.VaccineProduct.VaccineType.Name,
                record.VaccineProduct.VaccineType.Code,
                record.VaccineScheduleItemId,
                record.DoseNumber,
                record.AdministeredDate,
                record.AdministeredByUserId,
                record.BatchNumber,
                record.Notes,
                record.Status.ToString(),
                record.CreatedAt,
                record.UpdatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<VaccinationRecordResponse>(
            records,
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<VaccinationRecordResponse> GetRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();

        var record = await QueryRecordDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.Id == recordId &&
                    record.HospitalId == hospitalId &&
                    !record.IsDeleted,
                cancellationToken);

        if (record is null)
        {
            throw new NotFoundException("Vaccination record", recordId);
        }

        return ToResponse(record);
    }

    public async Task<VaccinationRecordResponse> CreateRecordAsync(
        CreateVaccinationRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();

        if (request.DoseNumber <= 0)
        {
            throw new ValidationException("Dose number must be greater than zero.");
        }

        if (request.AdministeredDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException("Administered date cannot be in the future.");
        }

        var patient = await GetPatientForCurrentHospitalAsync(
            request.PatientId,
            hospitalId,
            cancellationToken);

        var product = await GetActiveVaccineProductAsync(
            request.VaccineProductId,
            cancellationToken);

        await ValidateScheduleItemAsync(
            request.VaccineScheduleItemId,
            product.VaccineTypeId,
            request.DoseNumber,
            cancellationToken);

        await EnsureRecordIsNotDuplicateAsync(
            patient.Id,
            request.VaccineScheduleItemId,
            request.DoseNumber,
            ignoredRecordId: null,
            cancellationToken);

        var record = new VaccinationRecord
        {
            PatientId = patient.Id,
            HospitalId = hospitalId,
            VaccineProductId = product.Id,
            VaccineScheduleItemId = request.VaccineScheduleItemId,
            DoseNumber = request.DoseNumber,
            AdministeredDate = request.AdministeredDate,
            AdministeredByUserId = _currentUser.UserId,
            BatchNumber = TrimOrNull(request.BatchNumber),
            Notes = TrimOrNull(request.Notes),
            Status = VaccinationRecordStatus.Administered,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.VaccinationRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccination record {RecordId} created for patient {PatientId}.",
            record.Id,
            record.PatientId);

        return await GetRecordAsync(record.Id, cancellationToken);
    }

    public async Task<VaccinationRecordResponse> UpdateRecordAsync(
        Guid recordId,
        UpdateVaccinationRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();

        if (request.DoseNumber <= 0)
        {
            throw new ValidationException("Dose number must be greater than zero.");
        }

        if (request.AdministeredDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException("Administered date cannot be in the future.");
        }

        var record = await GetTrackedRecordForCurrentHospitalAsync(
            recordId,
            hospitalId,
            cancellationToken);

        if (record.Status != VaccinationRecordStatus.Administered)
        {
            throw new BusinessRuleException(
                "Only administered vaccination records can be updated.");
        }

        var product = await GetActiveVaccineProductAsync(
            request.VaccineProductId,
            cancellationToken);

        await ValidateScheduleItemAsync(
            request.VaccineScheduleItemId,
            product.VaccineTypeId,
            request.DoseNumber,
            cancellationToken);

        await EnsureRecordIsNotDuplicateAsync(
            record.PatientId,
            request.VaccineScheduleItemId,
            request.DoseNumber,
            ignoredRecordId: recordId,
            cancellationToken);

        record.VaccineProductId = product.Id;
        record.VaccineScheduleItemId = request.VaccineScheduleItemId;
        record.DoseNumber = request.DoseNumber;
        record.AdministeredDate = request.AdministeredDate;
        record.BatchNumber = TrimOrNull(request.BatchNumber);
        record.Notes = TrimOrNull(request.Notes);
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccination record {RecordId} updated.",
            record.Id);

        return await GetRecordAsync(record.Id, cancellationToken);
    }

    public async Task<VaccinationRecordResponse> CancelRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        return await SetRecordStatusAsync(
            recordId,
            VaccinationRecordStatus.Cancelled,
            cancellationToken);
    }

    public async Task<VaccinationRecordResponse> MarkRecordEnteredInErrorAsync(
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        return await SetRecordStatusAsync(
            recordId,
            VaccinationRecordStatus.EnteredInError,
            cancellationToken);
    }

    private async Task<VaccinationRecordResponse> SetRecordStatusAsync(
        Guid recordId,
        VaccinationRecordStatus status,
        CancellationToken cancellationToken)
    {
        var hospitalId = GetCurrentHospitalId();

        var record = await GetTrackedRecordForCurrentHospitalAsync(
            recordId,
            hospitalId,
            cancellationToken);

        if (record.Status == status)
        {
            throw new BusinessRuleException(
                $"Vaccination record is already {status}.");
        }

        if (record.Status != VaccinationRecordStatus.Administered)
        {
            throw new BusinessRuleException(
                "Only administered vaccination records can change status.");
        }

        record.Status = status;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vaccination record {RecordId} status changed to {Status}.",
            record.Id,
            record.Status);

        return await GetRecordAsync(record.Id, cancellationToken);
    }

    private Guid GetCurrentHospitalId()
    {
        if (!_currentUser.HospitalId.HasValue ||
            IsPlatformAdmin())
        {
            throw new ForbiddenException("Hospital access is required.");
        }

        return _currentUser.HospitalId.Value;
    }

    private bool IsPlatformAdmin()
    {
        return string.Equals(
            _currentUser.Role,
            Role.PlatformAdmin.ToString(),
            StringComparison.Ordinal);
    }

    private async Task<Patient> GetPatientForCurrentHospitalAsync(
        Guid patientId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(
                patient => patient.Id == patientId &&
                    patient.HospitalId == hospitalId &&
                    !patient.IsDeleted,
                cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException("Patient", patientId);
        }

        if (patient.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccination record cannot be created for an inactive patient.");
        }

        return patient;
    }

    private async Task<VaccineProduct> GetActiveVaccineProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.VaccineProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == productId &&
                    !product.IsDeleted,
                cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Vaccine product", productId);
        }

        if (product.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccination record cannot use an inactive vaccine product.");
        }

        return product;
    }

    private async Task ValidateScheduleItemAsync(
        Guid? scheduleItemId,
        Guid vaccineTypeId,
        int doseNumber,
        CancellationToken cancellationToken)
    {
        if (!scheduleItemId.HasValue)
        {
            return;
        }

        var scheduleItem = await _dbContext.VaccineScheduleItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == scheduleItemId.Value &&
                    !item.IsDeleted,
                cancellationToken);

        if (scheduleItem is null)
        {
            throw new NotFoundException("Vaccine schedule item", scheduleItemId.Value);
        }

        if (scheduleItem.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                "Vaccination record cannot use an inactive schedule item.");
        }

        if (scheduleItem.VaccineTypeId != vaccineTypeId)
        {
            throw new ValidationException(
                "Schedule item does not match the vaccine product type.");
        }

        if (scheduleItem.DoseNumber != doseNumber)
        {
            throw new ValidationException(
                "Dose number must match the selected schedule item.");
        }
    }

    private async Task EnsureRecordIsNotDuplicateAsync(
        Guid patientId,
        Guid? scheduleItemId,
        int doseNumber,
        Guid? ignoredRecordId,
        CancellationToken cancellationToken)
    {
        if (!scheduleItemId.HasValue)
        {
            return;
        }

        var exists = await _dbContext.VaccinationRecords
            .AnyAsync(
                record =>
                    !record.IsDeleted &&
                    record.Status == VaccinationRecordStatus.Administered &&
                    record.PatientId == patientId &&
                    record.VaccineScheduleItemId == scheduleItemId.Value &&
                    record.DoseNumber == doseNumber &&
                    (!ignoredRecordId.HasValue ||
                        record.Id != ignoredRecordId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "An administered vaccination record already exists for this patient, schedule item, and dose.");
        }
    }

    private async Task<VaccinationRecord> GetTrackedRecordForCurrentHospitalAsync(
        Guid recordId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var record = await _dbContext.VaccinationRecords
            .FirstOrDefaultAsync(
                record => record.Id == recordId &&
                    record.HospitalId == hospitalId &&
                    !record.IsDeleted,
                cancellationToken);

        if (record is null)
        {
            throw new NotFoundException("Vaccination record", recordId);
        }

        return record;
    }

    private IQueryable<VaccinationRecord> QueryRecordDetails()
    {
        return _dbContext.VaccinationRecords
            .Include(record => record.Patient)
            .Include(record => record.VaccineProduct)
                .ThenInclude(product => product.VaccineType);
    }

    private static bool TryParseRecordStatus(string status, out VaccinationRecordStatus value)
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

    private static VaccinationRecordResponse ToResponse(VaccinationRecord record)
    {
        return new VaccinationRecordResponse(
            record.Id,
            record.PatientId,
            record.Patient.PatientNumber,
            $"{record.Patient.FirstName} {record.Patient.LastName}",
            record.HospitalId,
            record.VaccineProductId,
            record.VaccineProduct.Name,
            record.VaccineProduct.Code,
            record.VaccineProduct.VaccineTypeId,
            record.VaccineProduct.VaccineType.Name,
            record.VaccineProduct.VaccineType.Code,
            record.VaccineScheduleItemId,
            record.DoseNumber,
            record.AdministeredDate,
            record.AdministeredByUserId,
            record.BatchNumber,
            record.Notes,
            record.Status.ToString(),
            record.CreatedAt,
            record.UpdatedAt);
    }
}
