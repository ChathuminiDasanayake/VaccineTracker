using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VaccineTracker.Application.Exceptions;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Patients;
using VaccineTracker.Domain.Entities;
using VaccineTracker.Domain.Enums;
using VaccineTracker.Infrastructure.Persistence;
using System.Security.Cryptography;
using VaccineTracker.Contracts.Common;

namespace VaccineTracker.Infrastructure.Services;

public sealed class PatientsService : IPatientsService
{
    private readonly VaccineTrackerDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PatientsService> _logger;

    private const string PatientNumberCharacters =
        "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public PatientsService(
        VaccineTrackerDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<PatientsService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResponse<PatientSummaryResponse>> GetPatientsAsync(
        PatientSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = ResolveHospitalId();

        if (!hospitalId.HasValue)
        {
            throw new ForbiddenException("Hospital access is required.");
        }

        EnsureHospitalAccess(hospitalId.Value);

        var query = _dbContext.Patients
            .AsNoTracking()
            .Where(patient =>
                patient.HospitalId == hospitalId.Value &&
                !patient.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.PatientNumber))
        {
            var patientNumber = request.PatientNumber.Trim();

            query = query.Where(patient =>
                patient.PatientNumber.Contains(patientNumber));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            query = query.Where(patient =>
                patient.FirstName.Contains(name) ||
                patient.LastName.Contains(name) ||
                (patient.FirstName + " " + patient.LastName).Contains(name));
        }

        if (request.DateOfBirth.HasValue)
        {
            query = query.Where(patient =>
                patient.DateOfBirth == request.DateOfBirth.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseEntityStatus(request.Status, out var status))
            {
                throw new ValidationException(
                    $"Status '{request.Status}' is invalid.");
            }

            query = query.Where(patient => patient.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var patients = await query
            .OrderBy(patient => patient.LastName)
            .ThenBy(patient => patient.FirstName)
            .ThenBy(patient => patient.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(patient => new PatientSummaryResponse(
                patient.Id,
                patient.PatientNumber,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Gender.ToString(),
                patient.Status.ToString()))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (totalCount + request.PageSize - 1) / request.PageSize;

        return new PagedResponse<PatientSummaryResponse>(
            patients,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<PatientSummaryResponse> GetPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await GetPatientEntityAsync(patientId, cancellationToken);

        return ToSummaryResponse(patient);
    }

    public async Task<PatientDetailsResponse> GetPatientDetailsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        EnsureSensitiveDataAccess();

        var patient = await GetPatientEntityAsync(patientId, cancellationToken);

        return ToDetailsResponse(patient);
    }

    public async Task<PatientSummaryResponse> CreatePatientAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = ResolveHospitalId();

        if (!hospitalId.HasValue)
        {
            throw new ForbiddenException("Hospital access is required.");
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

        var patientNumber = await GenerateUniquePatientNumberAsync(
            cancellationToken);

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

        return ToSummaryResponse(patient);
    }

    public async Task<PatientSummaryResponse> UpdatePatientAsync(
        Guid patientId,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(
                patient => patient.Id == patientId && !patient.IsDeleted,
                cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException("Patient", patientId);
        }

        EnsureHospitalAccess(patient.HospitalId);

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

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = gender;
        patient.NationalIdNumber = TrimOrNull(request.NationalIdNumber);
        patient.Email = TrimOrNull(request.Email);
        patient.PhoneNumber = TrimOrNull(request.PhoneNumber);
        patient.StreetAddress = TrimOrNull(request.StreetAddress);
        patient.City = TrimOrNull(request.City);
        patient.PostalCode = TrimOrNull(request.PostalCode);
        patient.Country = TrimOrNull(request.Country);
        patient.EmergencyContactName = TrimOrNull(request.EmergencyContactName);
        patient.EmergencyContactPhone = TrimOrNull(request.EmergencyContactPhone);
        patient.IsEmployee = request.IsEmployee;
        patient.UpdatedAt = DateTime.UtcNow;
        patient.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} updated for hospital {HospitalId}.",
            patient.Id,
            patient.HospitalId);

        return ToSummaryResponse(patient);
    }

    public async Task<PatientSummaryResponse> UpdatePatientStatusAsync(
        Guid patientId,
        UpdatePatientStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(
                patient => patient.Id == patientId && !patient.IsDeleted,
                cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException("Patient", patientId);
        }

        EnsureHospitalAccess(patient.HospitalId);

        if (!TryParseEntityStatus(request.Status, out var status))
        {
            throw new ValidationException(
                $"Status '{request.Status}' is invalid.");
        }

        if (patient.Status == status)
        {
            throw new BusinessRuleException(
                $"Patient is already {status}.");
        }   

        patient.Status = status;
        patient.UpdatedAt = DateTime.UtcNow;
        patient.UpdatedBy = _currentUser.UserId.ToString();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Patient {PatientId} status changed to {Status} for hospital {HospitalId}.",
            patient.Id,
            patient.Status,
            patient.HospitalId);

        return ToSummaryResponse(patient);
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

    private void EnsureSensitiveDataAccess()
    {
        var canViewSensitiveData =
            string.Equals(
                _currentUser.Role,
                Role.HospitalAdmin.ToString(),
                StringComparison.Ordinal) ||
            string.Equals(
                _currentUser.Role,
                Role.Doctor.ToString(),
                StringComparison.Ordinal) ||
            string.Equals(
                _currentUser.Role,
                Role.Nurse.ToString(),
                StringComparison.Ordinal);

        if (!canViewSensitiveData)
        {
            throw new ForbiddenException(
                "You cannot access sensitive patient data.");
        }
    }

    private static bool TryParseGender(string gender, out Gender value)
    {
        return Enum.TryParse(gender, ignoreCase: true, out value) &&
            Enum.IsDefined(value);
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

    private async Task<Patient> GetPatientEntityAsync(
        Guid patientId,
        CancellationToken cancellationToken)
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

        return patient;
    }

    private static PatientSummaryResponse ToSummaryResponse(Patient patient)
    {
        return new PatientSummaryResponse(
            patient.Id,
            patient.PatientNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.Status.ToString());
    }

    private static PatientDetailsResponse ToDetailsResponse(Patient patient)
    {
        return new PatientDetailsResponse(
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

    private async Task<string> GenerateUniquePatientNumberAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var patientNumber = GeneratePatientNumber();
            var exists = await _dbContext.Patients
                .AnyAsync(
                    patient => patient.PatientNumber == patientNumber,
                    cancellationToken);

            if (!exists)
            {
                return patientNumber;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique patient number.");
    }

    private static string GeneratePatientNumber()
    {
        Span<char> characters = stackalloc char[8];

        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] =
                PatientNumberCharacters[
                    RandomNumberGenerator.GetInt32(
                        PatientNumberCharacters.Length)];
        }

        var value = new string(characters);

        return $"PT-{value[..4]}-{value[4..]}";
    }
}
