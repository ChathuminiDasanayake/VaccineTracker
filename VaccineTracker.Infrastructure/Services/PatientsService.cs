using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Patients;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;

namespace VaccineTracker.Infrastructure.Services;

public sealed class PatientsService : IPatientsService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PatientsService> _logger;

    public PatientsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<PatientsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PatientResponse> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                patient => patient.Id == patientId && !patient.IsDeleted,
                cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException("Patient", patientId);
        }

        EnsureHospitalAccess(patient.HospitalId);

        return ToResponse(patient);
    }

    public async Task<PatientResponse> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = ResolveHospitalId();

        if (!hospitalId.HasValue)
        {
            throw new ValidationException("Hospital ID is required.");
        }

        EnsureHospitalAccess(hospitalId.Value);

        if (!TryParseGender(request.Gender, out var gender))
        {
            throw new ValidationException(
                $"Gender '{request.Gender}' is invalid.");
        }

        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException(
                "Date of birth cannot be in the future.");
        }

        var hospitalExists = await _dbContext.Hospitals
            .AsNoTracking()
            .AnyAsync(
                hospital => hospital.Id == hospitalId.Value &&
                    hospital.IsActive &&
                    !hospital.IsDeleted,
                cancellationToken);

        if (!hospitalExists)
        {
            throw new NotFoundException("Active hospital", hospitalId.Value);
        }

        var patientNumber = request.PatientNumber.Trim();
        var patientNumberExists = await _dbContext.Patients
            .AnyAsync(
                patient => patient.HospitalId == hospitalId.Value &&
                    patient.PatientNumber == patientNumber &&
                    !patient.IsDeleted,
                cancellationToken);

        if (patientNumberExists)
        {
            throw new ConflictException(
                $"Patient number '{patientNumber}' already exists in this hospital.");
        }

        var patient = new Patient
        {
            HospitalId = hospitalId.Value,
            PatientNumber = patientNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = gender,
            NationalIdNumber = TrimOrNull(request.NationalIdNumber),
            Email = TrimOrNull(request.Email),
            PhoneNumber = TrimOrNull(request.PhoneNumber),
            StreetAddress = TrimOrNull(request.StreetAddress),
            City = TrimOrNull(request.City),
            PostalCode = TrimOrNull(request.PostalCode),
            Country = TrimOrNull(request.Country),
            EmergencyContactName = TrimOrNull(request.EmergencyContactName),
            EmergencyContactPhone = TrimOrNull(request.EmergencyContactPhone),
            IsEmployee = request.IsEmployee,
            Status = EntityStatus.Active,
            CreatedBy = _currentUser.UserId.ToString()
        };

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} created for hospital {HospitalId}.",
            patient.Id,
            patient.HospitalId);

        return ToResponse(patient);
    }

    private Guid? ResolveHospitalId()
    {
        return _currentUser.HospitalId;
    }

    private void EnsureHospitalAccess(Guid hospitalId)
    {
        if (IsPlatformAdmin() ||
            !_currentUser.HospitalId.HasValue ||
            _currentUser.HospitalId.Value != hospitalId)
        {
            throw new ForbiddenException(
                $"You cannot access hospital '{hospitalId}'.");
        }
    }

    private bool IsPlatformAdmin()
    {
        return string.Equals(
            _currentUser.Role,
            Role.PlatformAdmin.ToString(),
            StringComparison.Ordinal);
    }

    private static bool TryParseGender(string gender, out Gender value)
    {
        return Enum.TryParse(gender, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static PatientResponse ToResponse(Patient patient)
    {
        return new PatientResponse(
            patient.Id,
            patient.HospitalId,
            patient.PatientNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.NationalIdNumber,
            patient.Email,
            patient.PhoneNumber,
            patient.StreetAddress,
            patient.City,
            patient.PostalCode,
            patient.Country,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone,
            patient.IsEmployee,
            patient.Status.ToString(),
            patient.CreatedAt,
            patient.UpdatedAt);
    }
}
