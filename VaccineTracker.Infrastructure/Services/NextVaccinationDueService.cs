using Microsoft.EntityFrameworkCore;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.VaccinationRecords;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class NextVaccinationDueService : INextVaccinationDueService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public NextVaccinationDueService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<NextVaccinationDueResponse?> GetNextDueForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var patient = await GetActivePatientAsync(
            patientId,
            hospitalId,
            cancellationToken);

        var targetGroup = ResolveTargetGroup(patient.DateOfBirth, today);

        var administeredRecords = await GetAdministeredRecordsForPatientAsync(
            patient.Id,
            hospitalId,
            cancellationToken);

        var completedScheduleItemIds = administeredRecords
            .Where(record => record.VaccineScheduleItemId.HasValue)
            .Select(record => record.VaccineScheduleItemId!.Value)
            .ToHashSet();

        var scheduleItems = await _dbContext.VaccineScheduleItems
            .AsNoTracking()
            .Include(item => item.VaccineType)
            .Where(item =>
                !item.IsDeleted &&
                item.Status == EntityStatus.Active &&
                item.TargetGroup == targetGroup)
            .OrderBy(item => item.DoseNumber)
            .ThenBy(item => item.DueAgeInDays)
            .ToListAsync(cancellationToken);

        var candidates = scheduleItems
            .Where(item => !completedScheduleItemIds.Contains(item.Id))
            .Select(item => new
            {
                ScheduleItem = item,
                DueDate = CalculateDueDate(
                    item,
                    patient.DateOfBirth,
                    GetLastAdministeredDateForVaccineType(
                        administeredRecords,
                        item.VaccineTypeId))
            })
            .Where(candidate => candidate.DueDate.HasValue)
            .OrderBy(candidate => candidate.DueDate!.Value)
            .ThenBy(candidate => candidate.ScheduleItem.DoseNumber)
            .FirstOrDefault();

        return candidates is null
            ? null
            : ToResponse(patient, candidates.ScheduleItem, candidates.DueDate!.Value, today);
    }

    public async Task<NextVaccinationDueResponse?> GetNextDueAfterRecordAsync(
        Guid vaccinationRecordId,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = GetCurrentHospitalId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var record = await _dbContext.VaccinationRecords
            .AsNoTracking()
            .Include(record => record.Patient)
            .Include(record => record.VaccineProduct)
                .ThenInclude(product => product.VaccineType)
            .Include(record => record.VaccineScheduleItem)
            .FirstOrDefaultAsync(
                record => record.Id == vaccinationRecordId &&
                    record.HospitalId == hospitalId &&
                    !record.IsDeleted,
                cancellationToken);

        if (record is null)
        {
            throw new NotFoundException("Vaccination record", vaccinationRecordId);
        }

        if (record.Status != VaccinationRecordStatus.Administered)
        {
            return null;
        }

        var targetGroup = record.VaccineScheduleItem?.TargetGroup ??
            ResolveTargetGroup(record.Patient.DateOfBirth, record.AdministeredDate);

        var administeredRecords = await GetAdministeredRecordsForPatientAsync(
            record.PatientId,
            hospitalId,
            cancellationToken);

        var completedScheduleItemIds = administeredRecords
            .Where(administeredRecord => administeredRecord.VaccineScheduleItemId.HasValue)
            .Select(administeredRecord => administeredRecord.VaccineScheduleItemId!.Value)
            .ToHashSet();

        var nextScheduleItem = await _dbContext.VaccineScheduleItems
            .AsNoTracking()
            .Include(item => item.VaccineType)
            .Where(item =>
                !item.IsDeleted &&
                item.Status == EntityStatus.Active &&
                item.VaccineTypeId == record.VaccineProduct.VaccineTypeId &&
                item.TargetGroup == targetGroup &&
                item.DoseNumber > record.DoseNumber)
            .OrderBy(item => item.DoseNumber)
            .FirstOrDefaultAsync(
                item => !completedScheduleItemIds.Contains(item.Id),
                cancellationToken);

        if (nextScheduleItem is null)
        {
            return null;
        }

        var dueDate = CalculateDueDate(
            nextScheduleItem,
            record.Patient.DateOfBirth,
            record.AdministeredDate);

        return dueDate.HasValue
            ? ToResponse(record.Patient, nextScheduleItem, dueDate.Value, today)
            : null;
    }

    private Guid GetCurrentHospitalId()
    {
        if (!_currentUser.HospitalId.HasValue ||
            string.Equals(_currentUser.Role, Role.PlatformAdmin.ToString(), StringComparison.Ordinal))
        {
            throw new ForbiddenException("Hospital access is required.");
        }

        return _currentUser.HospitalId.Value;
    }

    private async Task<Patient> GetActivePatientAsync(
        Guid patientId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
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
                "Next due vaccination cannot be calculated for an inactive patient.");
        }

        return patient;
    }

    private async Task<List<VaccinationRecord>> GetAdministeredRecordsForPatientAsync(
        Guid patientId,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.VaccinationRecords
            .AsNoTracking()
            .Include(record => record.VaccineProduct)
            .Where(record =>
                record.PatientId == patientId &&
                record.HospitalId == hospitalId &&
                !record.IsDeleted &&
                record.Status == VaccinationRecordStatus.Administered)
            .ToListAsync(cancellationToken);
    }

    private static DateOnly? CalculateDueDate(
        VaccineScheduleItem scheduleItem,
        DateOnly dateOfBirth,
        DateOnly? lastAdministeredDate)
    {
        if (scheduleItem.DueAgeInDays.HasValue)
        {
            return dateOfBirth.AddDays(scheduleItem.DueAgeInDays.Value);
        }

        if (scheduleItem.RepeatIntervalInDays.HasValue &&
            lastAdministeredDate.HasValue)
        {
            return lastAdministeredDate.Value.AddDays(
                scheduleItem.RepeatIntervalInDays.Value);
        }

        if (scheduleItem.MinimumAgeInYears.HasValue)
        {
            return dateOfBirth.AddYears(scheduleItem.MinimumAgeInYears.Value);
        }

        return null;
    }

    private static DateOnly? GetLastAdministeredDateForVaccineType(
        IEnumerable<VaccinationRecord> records,
        Guid vaccineTypeId)
    {
        return records
            .Where(record => record.VaccineProduct.VaccineTypeId == vaccineTypeId)
            .OrderByDescending(record => record.AdministeredDate)
            .Select(record => (DateOnly?)record.AdministeredDate)
            .FirstOrDefault();
    }

    private static VaccineTargetGroup ResolveTargetGroup(
        DateOnly dateOfBirth,
        DateOnly referenceDate)
    {
        var age = referenceDate.Year - dateOfBirth.Year;

        if (dateOfBirth > referenceDate.AddYears(-age))
        {
            age--;
        }

        return age switch
        {
            < 18 => VaccineTargetGroup.Child,
            >= 60 => VaccineTargetGroup.Elder,
            _ => VaccineTargetGroup.Adult
        };
    }

    private static NextVaccinationDueResponse ToResponse(
        Patient patient,
        VaccineScheduleItem scheduleItem,
        DateOnly dueDate,
        DateOnly today)
    {
        var daysUntilDue = dueDate.DayNumber - today.DayNumber;

        return new NextVaccinationDueResponse(
            patient.Id,
            patient.PatientNumber,
            scheduleItem.VaccineTypeId,
            scheduleItem.VaccineType.Name,
            scheduleItem.VaccineType.Code,
            scheduleItem.Id,
            scheduleItem.TargetGroup.ToString(),
            scheduleItem.DoseNumber,
            dueDate,
            daysUntilDue,
            daysUntilDue < 0,
            scheduleItem.Description);
    }
}
